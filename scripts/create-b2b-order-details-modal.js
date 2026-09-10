const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/components/B2BOrderDetailsModal.tsx');

const content = `import React, { useState, useEffect } from 'react';
import {
  useB2BOrder,
  useApproveB2BOrder,
  useInvoiceB2BOrder,
  useRejectB2BOrder,
} from '../hooks/useB2BOrders';
import { useMerchant } from '../hooks/useMerchants';
import { buildOrderWhatsAppUrl } from '../utils/whatsappUtils';
import {
  X,
  CheckCircle2,
  FileText,
  XCircle,
  MessageCircle,
  AlertTriangle,
  Clock,
  ShieldAlert,
  Loader2,
  Building2,
  Phone,
  Receipt,
  User,
} from 'lucide-react';
import toast from 'react-hot-toast';

interface B2BOrderDetailsModalProps {
  orderId: string | null;
  isOpen: boolean;
  onClose: () => void;
}

export const B2BOrderDetailsModal: React.FC<B2BOrderDetailsModalProps> = ({
  orderId,
  isOpen,
  onClose,
}) => {
  const { data: order, isLoading } = useB2BOrder(orderId || undefined);
  const { data: merchant } = useMerchant(order?.merchantId);
  const approveMutation = useApproveB2BOrder();
  const invoiceMutation = useInvoiceB2BOrder();
  const rejectMutation = useRejectB2BOrder();

  // Local adjustments state: { [itemId]: { approvedQuantity: number, adjustmentReason: string } }
  const [adjustments, setAdjustments] = useState<
    Record<string, { approvedQuantity: number; adjustmentReason: string }>
  >({});

  // Reject state
  const [isRejecting, setIsRejecting] = useState(false);
  const [rejectionReason, setRejectionReason] = useState('');

  // Invoice state
  const [isInvoiceConfirmOpen, setIsInvoiceConfirmOpen] = useState(false);
  const [cashPaidAmount, setCashPaidAmount] = useState<number>(0);
  const [invoiceNotes, setInvoiceNotes] = useState('');

  // Credit Limit Override state (on 409)
  const [creditBreach, setCreditBreach] = useState<{
    currentBalance: number;
    newCreditAmount: number;
    totalProjectedBalance: number;
    creditLimit: number;
    excessAmount: number;
  } | null>(null);
  const [creditOverrideConfirmed, setCreditOverrideConfirmed] = useState(false);
  const [creditOverrideReason, setCreditOverrideReason] = useState('');

  // Reset state when order changes
  useEffect(() => {
    if (order) {
      const initial: Record<string, { approvedQuantity: number; adjustmentReason: string }> = {};
      order.items.forEach((item) => {
        initial[item.id] = {
          approvedQuantity: item.approvedQuantity ?? item.requestedQuantity,
          adjustmentReason: item.adjustmentReason || '',
        };
      });
      setAdjustments(initial);
      setIsRejecting(false);
      setRejectionReason('');
      setIsInvoiceConfirmOpen(false);
      setCashPaidAmount(0);
      setInvoiceNotes('');
      setCreditBreach(null);
      setCreditOverrideConfirmed(false);
      setCreditOverrideReason('');
    }
  }, [order]);

  if (!isOpen) return null;

  const handleQtyChange = (itemId: string, val: number) => {
    setAdjustments((prev) => ({
      ...prev,
      [itemId]: {
        approvedQuantity: Math.max(0, val),
        adjustmentReason: prev[itemId]?.adjustmentReason || '',
      },
    }));
  };

  const handleReasonChange = (itemId: string, reason: string) => {
    setAdjustments((prev) => ({
      ...prev,
      [itemId]: {
        approvedQuantity: prev[itemId]?.approvedQuantity ?? 0,
        adjustmentReason: reason,
      },
    }));
  };

  const handleApprove = async () => {
    if (!order) return;

    // Check if any reduced items lack an adjustment reason
    for (const item of order.items) {
      const adj = adjustments[item.id];
      if (adj && adj.approvedQuantity < item.requestedQuantity && !adj.adjustmentReason?.trim()) {
        toast.error(\`يرجى كتابة سبب تعديل كمية الصنف: \${item.productName}\`);
        return;
      }
    }

    const payload = {
      items: order.items.map((item) => ({
        orderItemId: item.id,
        approvedQuantity: adjustments[item.id]?.approvedQuantity ?? item.approvedQuantity ?? item.requestedQuantity,
        adjustmentReason: adjustments[item.id]?.adjustmentReason || undefined,
      })),
    };

    try {
      await approveMutation.mutateAsync({ id: order.id, data: payload });
    } catch {
      // Toast handled by mutation
    }
  };

  const handleReject = async () => {
    if (!order) return;
    if (!rejectionReason.trim()) {
      toast.error('يرجى تحديد سبب رفض الطلب');
      return;
    }

    try {
      await rejectMutation.mutateAsync({ id: order.id, reason: rejectionReason });
      setIsRejecting(false);
    } catch {
      // Handled
    }
  };

  const handleInvoiceSubmit = async () => {
    if (!order) return;

    if (creditBreach && !creditOverrideConfirmed) {
      toast.error('يجب تأكيد الموافقة على تجاوز سقف الائتمان أولاً');
      return;
    }

    if (creditBreach && creditOverrideConfirmed && creditOverrideReason.trim().length < 10) {
      toast.error('يرجى كتابة سبب مفصل لتجاوز الائتمان (10 أحرف على الأقل)');
      return;
    }

    const paymentMethod =
      cashPaidAmount >= order.totalAmount
        ? 'Cash'
        : cashPaidAmount > 0
        ? 'Mixed'
        : 'Credit';

    const payload = {
      paidAmount: cashPaidAmount,
      paymentMethod,
      notes: invoiceNotes || undefined,
      creditLimitOverrideConfirmed: creditOverrideConfirmed,
      creditLimitOverrideReason: creditOverrideReason || undefined,
    };

    try {
      await invoiceMutation.mutateAsync({ id: order.id, data: payload });
      setIsInvoiceConfirmOpen(false);
      setCreditBreach(null);
    } catch (err: any) {
      if (err.response?.status === 409 && err.response?.data?.code === 'CREDIT_LIMIT_EXCEEDED') {
        const breachData = err.response.data.data;
        setCreditBreach(breachData);
        toast.error('تم تجاوز سقف الائتمان المسموح به لهذا التاجر!');
      } else {
        const msg = err.response?.data?.message || 'تعذر إصدار فاتورة المبيعات';
        toast.error(msg);
      }
    }
  };

  // WhatsApp click-to-chat
  const whatsappUrl = order?.merchantPhone
    ? buildOrderWhatsAppUrl(
        order.merchantPhone,
        order.orderNumber,
        order.merchantTradeName,
        order.status,
        order.totalAmount,
        order.paymentPreference
      )
    : null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm overflow-y-auto">
      <div className="bg-white dark:bg-slate-800 rounded-2xl max-w-4xl w-full border border-slate-200 dark:border-slate-700 shadow-2xl overflow-hidden my-8 animate-in fade-in zoom-in-95 duration-150">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary-100 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400 rounded-xl">
              <Receipt className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-lg font-bold text-slate-900 dark:text-white">
                  طلب توريد بالجملة #{order?.orderNumber || '...'}
                </h3>
                {order && (
                  <span
                    className={\`px-2.5 py-0.5 rounded-full text-xs font-bold \${
                      order.status === 'Pending'
                        ? 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300'
                        : order.status === 'Approved'
                        ? 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300'
                        : order.status === 'Invoiced'
                        ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300'
                        : order.status === 'Rejected'
                        ? 'bg-rose-100 text-rose-800 dark:bg-rose-900/40 dark:text-rose-300'
                        : 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300'
                    }\`}
                  >
                    {order.status === 'Pending'
                      ? 'قيد المراجعة'
                      : order.status === 'Approved'
                      ? 'معتمد للتوريد'
                      : order.status === 'Invoiced'
                      ? 'تمت الفوترة والخصم'
                      : order.status === 'Rejected'
                      ? 'مرفوض'
                      : 'ملغي'}
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                {order?.createdAt ? new Date(order.createdAt).toLocaleString('ar-EG') : ''}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            {whatsappUrl && (
              <a
                href={whatsappUrl}
                target="_blank"
                rel="noopener noreferrer"
                title="فتح محادثة واتساب مع التاجر برسالة جاهزة"
                className="flex items-center gap-1.5 px-3 py-1.5 bg-emerald-500 hover:bg-emerald-600 text-white rounded-xl text-xs font-bold shadow-sm transition-colors"
              >
                <MessageCircle className="w-4 h-4" />
                <span>واتساب التاجر</span>
              </a>
            )}
            <button
              type="button"
              onClick={onClose}
              className="p-1.5 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded-lg"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Modal Body */}
        <div className="p-6 space-y-6 max-h-[75vh] overflow-y-auto">
          {isLoading ? (
            <div className="flex flex-col items-center justify-center py-12">
              <Loader2 className="w-8 h-8 text-primary-600 animate-spin mb-2" />
              <p className="text-sm text-slate-500">جاري تحميل تفاصيل الطلب...</p>
            </div>
          ) : !order ? (
            <div className="text-center py-12 text-slate-500">لم يتم العثور على بيانات الطلب</div>
          ) : (
            <>
              {/* Merchant Info & Order Meta Card */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 bg-slate-50 dark:bg-slate-900/40 p-4 rounded-xl border border-slate-200 dark:border-slate-700">
                <div className="space-y-2">
                  <div className="flex items-center gap-2 text-slate-900 dark:text-white font-bold text-sm">
                    <Building2 className="w-4 h-4 text-primary-500" />
                    <span>التاجر: {order.merchantTradeName || merchant?.tradeName || 'غير محدد'}</span>
                  </div>
                  <div className="flex items-center gap-2 text-xs text-slate-600 dark:text-slate-400">
                    <User className="w-3.5 h-3.5 text-slate-400" />
                    <span>المسؤول: {merchant?.contactPerson || 'غير محدد'}</span>
                  </div>
                  <div className="flex items-center gap-2 text-xs text-slate-600 dark:text-slate-400">
                    <Phone className="w-3.5 h-3.5 text-slate-400" />
                    <span dir="ltr">{order.merchantPhone || merchant?.phone || ''}</span>
                  </div>
                </div>

                <div className="space-y-2 border-t md:border-t-0 md:border-r border-slate-200 dark:border-slate-700 md:pr-4">
                  <div className="flex items-center justify-between text-xs">
                    <span className="text-slate-500">طريقة الدفع المفضلة:</span>
                    <span className="font-bold text-slate-800 dark:text-slate-200">
                      {order.paymentPreference === 'Cash'
                        ? 'نقدي بالكامل'
                        : order.paymentPreference === 'Credit'
                        ? 'آجل بالكامل على الحساب'
                        : 'جزئي (دفعة نقدية + آجل)'}
                    </span>
                  </div>
                  {merchant && (
                    <>
                      <div className="flex items-center justify-between text-xs">
                        <span className="text-slate-500">سقف الائتمان:</span>
                        <span className="font-bold text-slate-800 dark:text-slate-200">
                          {merchant.creditLimit.toLocaleString('ar-EG')} ج.م
                        </span>
                      </div>
                      <div className="flex items-center justify-between text-xs">
                        <span className="text-slate-500">الرصيد القائم حالياً:</span>
                        <span
                          className={\`font-bold \${
                            merchant.currentBalance > merchant.creditLimit
                              ? 'text-rose-600 font-extrabold'
                              : 'text-slate-800 dark:text-slate-200'
                          }\`}
                        >
                          {merchant.currentBalance.toLocaleString('ar-EG')} ج.م
                        </span>
                      </div>
                    </>
                  )}
                </div>
              </div>

              {/* Status alerts */}
              {order.status === 'Invoiced' && order.salesInvoiceId && (
                <div className="flex items-center gap-3 p-3 bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800 rounded-xl text-emerald-800 dark:text-emerald-300 text-sm">
                  <CheckCircle2 className="w-5 h-5 flex-shrink-0" />
                  <div>
                    <span className="font-bold">تمت فوترة هذا الطلب بنجاح: </span>
                    <span>تم إنشاء فاتورة مبيعات وخصم المخزون بنجاح</span>
                  </div>
                </div>
              )}

              {order.status === 'Rejected' && (
                <div className="flex items-start gap-3 p-3 bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 rounded-xl text-rose-800 dark:text-rose-300 text-sm">
                  <XCircle className="w-5 h-5 flex-shrink-0 mt-0.5" />
                  <div>
                    <span className="font-bold">سبب رفض الطلب: </span>
                    <span>{order.rejectionReason || 'لم يتم توثيق سبب'}</span>
                  </div>
                </div>
              )}

              {order.status === 'Cancelled' && (
                <div className="flex items-start gap-3 p-3 bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800 rounded-xl text-amber-800 dark:text-amber-300 text-sm">
                  <Clock className="w-5 h-5 flex-shrink-0 mt-0.5" />
                  <div>
                    <span className="font-bold">تم إلغاء الطلب: </span>
                    <span>{order.cancellationReason || 'ألغي من قبل التاجر'}</span>
                  </div>
                </div>
              )}

              {/* Order Items Table */}
              <div>
                <h4 className="font-bold text-slate-800 dark:text-slate-200 text-sm mb-3">
                  الأصناف المطلوبة والكميات المعتمدة
                </h4>
                <div className="border border-slate-200 dark:border-slate-700 rounded-xl overflow-hidden">
                  <table className="w-full text-sm text-right">
                    <thead className="bg-slate-100 dark:bg-slate-700/60 text-slate-600 dark:text-slate-300 text-xs uppercase font-bold">
                      <tr>
                        <th className="px-3 py-2.5">الصنف</th>
                        <th className="px-3 py-2.5 text-center">الكمية المطلوبة</th>
                        <th className="px-3 py-2.5 text-center">سعر الوحدة</th>
                        <th className="px-3 py-2.5 text-center">الكمية المعتمدة</th>
                        <th className="px-3 py-2.5 text-left">الإجمالي</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                      {order.items.map((item) => {
                        const adj = adjustments[item.id] || {
                          approvedQuantity: item.approvedQuantity ?? item.requestedQuantity,
                          adjustmentReason: item.adjustmentReason || '',
                        };
                        const isPending = order.status === 'Pending';
                        const isReduced = adj.approvedQuantity < item.requestedQuantity;
                        const lineTotal = adj.approvedQuantity * item.unitWholesalePrice;

                        return (
                          <React.Fragment key={item.id}>
                            <tr className="hover:bg-slate-50/50 dark:hover:bg-slate-700/30 transition-colors">
                              <td className="px-3 py-3 font-semibold text-slate-900 dark:text-white">
                                {item.productName}
                              </td>
                              <td className="px-3 py-3 text-center text-slate-700 dark:text-slate-300 font-bold">
                                {item.requestedQuantity}
                              </td>
                              <td className="px-3 py-3 text-center text-slate-700 dark:text-slate-300">
                                {item.unitWholesalePrice.toLocaleString('ar-EG')} ج.م
                              </td>
                              <td className="px-3 py-3 text-center">
                                {isPending ? (
                                  <input
                                    type="number"
                                    min="0"
                                    value={adj.approvedQuantity}
                                    onChange={(e) =>
                                      handleQtyChange(item.id, parseFloat(e.target.value) || 0)
                                    }
                                    className={\`w-20 px-2 py-1 text-center font-bold rounded-lg border text-sm \${
                                      isReduced
                                        ? 'border-amber-400 bg-amber-50 dark:bg-amber-950/40 text-amber-900 dark:text-amber-200'
                                        : 'border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-white'
                                    }\`}
                                  />
                                ) : (
                                  <span
                                    className={\`font-bold \${
                                      isReduced ? 'text-amber-600 dark:text-amber-400' : ''
                                    }\`}
                                  >
                                    {item.approvedQuantity ?? item.requestedQuantity}
                                  </span>
                                )}
                              </td>
                              <td className="px-3 py-3 text-left font-bold text-slate-900 dark:text-white">
                                {lineTotal.toLocaleString('ar-EG')} ج.م
                              </td>
                            </tr>

                            {/* Adjustment Reason row if reduced or previously recorded */}
                            {(isReduced || item.adjustmentReason) && (
                              <tr className="bg-amber-50/50 dark:bg-amber-950/20">
                                <td colSpan={5} className="px-3 py-2 text-xs">
                                  <div className="flex items-center gap-2 text-amber-800 dark:text-amber-300">
                                    <AlertTriangle className="w-3.5 h-3.5 flex-shrink-0" />
                                    <span className="font-semibold">
                                      تم تعديل الكمية (المطلوب {item.requestedQuantity} ← المعتمد{' '}
                                      {adj.approvedQuantity}):
                                    </span>
                                    {isPending ? (
                                      <input
                                        type="text"
                                        placeholder="سبب النقص أو التعديل (إلزامي للتوثيق)..."
                                        value={adj.adjustmentReason || ''}
                                        onChange={(e) => handleReasonChange(item.id, e.target.value)}
                                        className="flex-1 px-2 py-0.5 rounded border border-amber-300 dark:border-amber-700 bg-white dark:bg-slate-800 text-slate-900 dark:text-white text-xs"
                                      />
                                    ) : (
                                      <span className="italic">
                                        {item.adjustmentReason || 'نقص في المخزون'}
                                      </span>
                                    )}
                                  </div>
                                </td>
                              </tr>
                            )}
                          </React.Fragment>
                        );
                      })}
                    </tbody>
                    <tfoot className="bg-slate-50 dark:bg-slate-900/60 font-bold border-t border-slate-200 dark:border-slate-700">
                      <tr>
                        <td colSpan={4} className="px-3 py-3 text-left text-slate-700 dark:text-slate-300">
                          إجمالي قيمة الطلب:
                        </td>
                        <td className="px-3 py-3 text-left text-primary-600 dark:text-primary-400 text-base">
                          {order.totalAmount.toLocaleString('ar-EG')} ج.م
                        </td>
                      </tr>
                    </tfoot>
                  </table>
                </div>
              </div>

              {/* Invoice Conversion Confirmation & Credit Override Panel */}
              {isInvoiceConfirmOpen && (
                <div className="p-4 bg-primary-50/70 dark:bg-primary-950/30 border border-primary-200 dark:border-primary-800 rounded-xl space-y-4">
                  <div className="flex items-center justify-between">
                    <h4 className="font-bold text-primary-950 dark:text-primary-100 text-sm flex items-center gap-2">
                      <FileText className="w-4 h-4" />
                      <span>تأكيد تحويل الطلب إلى فاتورة مبيعات رسمية</span>
                    </h4>
                    <button
                      type="button"
                      onClick={() => setIsInvoiceConfirmOpen(false)}
                      className="text-xs text-slate-400 hover:text-slate-600"
                    >
                      إلغاء
                    </button>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                        المدفوع نقداً الآن (ج.م):
                      </label>
                      <input
                        type="number"
                        min="0"
                        max={order.totalAmount}
                        value={cashPaidAmount}
                        onChange={(e) => setCashPaidAmount(parseFloat(e.target.value) || 0)}
                        className="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm font-bold"
                      />
                      <p className="text-[11px] text-slate-500 mt-1">
                        المبلغ الآجل المتبقي على الحساب:{' '}
                        <span className="font-bold text-slate-800 dark:text-slate-200">
                          {Math.max(0, order.totalAmount - cashPaidAmount).toLocaleString('ar-EG')}{' '}
                          ج.م
                        </span>
                      </p>
                    </div>

                    <div>
                      <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                        ملاحظات الفاتورة (اختياري):
                      </label>
                      <input
                        type="text"
                        placeholder="أي تفاصيل تسليم أو شحن..."
                        value={invoiceNotes}
                        onChange={(e) => setInvoiceNotes(e.target.value)}
                        className="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm"
                      />
                    </div>
                  </div>

                  {/* 409 Credit Limit Override Section */}
                  {creditBreach && (
                    <div className="p-4 bg-rose-50 dark:bg-rose-950/50 border border-rose-300 dark:border-rose-800 rounded-xl space-y-3">
                      <div className="flex items-center gap-2 text-rose-800 dark:text-rose-200 font-bold text-sm">
                        <ShieldAlert className="w-5 h-5 text-rose-600 flex-shrink-0" />
                        <span>تحذير رقابي: هذه الفاتورة تتجاوز سقف الائتمان المسموح به!</span>
                      </div>

                      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 text-xs bg-white/70 dark:bg-slate-800/70 p-2.5 rounded-lg">
                        <div>
                          <span className="text-slate-500 block">الرصيد القائم:</span>
                          <span className="font-bold text-slate-800 dark:text-slate-100">
                            {creditBreach.currentBalance.toLocaleString('ar-EG')} ج.م
                          </span>
                        </div>
                        <div>
                          <span className="text-slate-500 block">الآجل الجديد:</span>
                          <span className="font-bold text-slate-800 dark:text-slate-100">
                            {creditBreach.newCreditAmount.toLocaleString('ar-EG')} ج.م
                          </span>
                        </div>
                        <div>
                          <span className="text-slate-500 block">سقف الائتمان:</span>
                          <span className="font-bold text-slate-800 dark:text-slate-100">
                            {creditBreach.creditLimit.toLocaleString('ar-EG')} ج.م
                          </span>
                        </div>
                        <div>
                          <span className="text-rose-600 block font-semibold">مبلغ التجاوز:</span>
                          <span className="font-black text-rose-600">
                            +{creditBreach.excessAmount.toLocaleString('ar-EG')} ج.م
                          </span>
                        </div>
                      </div>

                      <label className="flex items-start gap-2.5 cursor-pointer pt-1">
                        <input
                          type="checkbox"
                          checked={creditOverrideConfirmed}
                          onChange={(e) => setCreditOverrideConfirmed(e.target.checked)}
                          className="mt-0.5 rounded text-rose-600 focus:ring-rose-500 w-4 h-4"
                        />
                        <span className="text-xs font-bold text-slate-900 dark:text-slate-100">
                          أؤكد بصفتي مسؤولاً الموافقة على استثناء وتجاوز سقف الائتمان لهذا التاجر على
                          مسؤوليتي.
                        </span>
                      </label>

                      {creditOverrideConfirmed && (
                        <div>
                          <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                            مبرر التجاوز الرقابي (إلزامي - 10 أحرف كحد أدنى):
                          </label>
                          <textarea
                            rows={2}
                            placeholder="مثال: تم الاتفاق مع التاجر على سداد المبلغ نقداً عند التسليم غداً..."
                            value={creditOverrideReason}
                            onChange={(e) => setCreditOverrideReason(e.target.value)}
                            className="w-full px-3 py-2 rounded-xl border border-rose-300 dark:border-rose-700 bg-white dark:bg-slate-800 text-xs"
                          />
                        </div>
                      )}
                    </div>
                  )}

                  <div className="flex justify-end gap-2 pt-2">
                    <button
                      type="button"
                      onClick={() => setIsInvoiceConfirmOpen(false)}
                      className="px-4 py-2 text-xs font-bold text-slate-600 dark:text-slate-300 hover:bg-slate-200 dark:hover:bg-slate-700 rounded-xl"
                    >
                      تراجع
                    </button>
                    <button
                      type="button"
                      disabled={invoiceMutation.isPending}
                      onClick={handleInvoiceSubmit}
                      className="flex items-center gap-1.5 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold shadow-md transition-colors"
                    >
                      {invoiceMutation.isPending ? (
                        <Loader2 className="w-4 h-4 animate-spin" />
                      ) : (
                        <CheckCircle2 className="w-4 h-4" />
                      )}
                      <span>
                        {creditBreach ? 'تأكيد وإصدار الفاتورة بالاستثناء' : 'تأكيد إصدار الفاتورة'}
                      </span>
                    </button>
                  </div>
                </div>
              )}

              {/* Rejection Prompt */}
              {isRejecting && (
                <div className="p-4 bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 rounded-xl space-y-3">
                  <h4 className="font-bold text-rose-900 dark:text-rose-100 text-sm">
                    رفض طلب التوريد
                  </h4>
                  <textarea
                    rows={2}
                    placeholder="يرجى كتابة سبب رفض الطلب..."
                    value={rejectionReason}
                    onChange={(e) => setRejectionReason(e.target.value)}
                    className="w-full px-3 py-2 rounded-xl border border-rose-300 dark:border-rose-700 bg-white dark:bg-slate-800 text-xs"
                  />
                  <div className="flex justify-end gap-2">
                    <button
                      type="button"
                      onClick={() => setIsRejecting(false)}
                      className="px-3 py-1.5 text-xs text-slate-600 dark:text-slate-300"
                    >
                      إلغاء
                    </button>
                    <button
                      type="button"
                      disabled={rejectMutation.isPending}
                      onClick={handleReject}
                      className="px-3 py-1.5 bg-rose-600 hover:bg-rose-700 text-white text-xs font-bold rounded-lg"
                    >
                      {rejectMutation.isPending ? 'جاري الرفض...' : 'تأكيد الرفض'}
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>

        {/* Footer Actions */}
        {order && (
          <div className="px-6 py-4 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-200 dark:border-slate-700 flex items-center justify-between">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 text-xs font-bold text-slate-600 dark:text-slate-400 hover:text-slate-800"
            >
              إغلاق
            </button>

            <div className="flex items-center gap-2">
              {order.status === 'Pending' && !isRejecting && (
                <>
                  <button
                    type="button"
                    onClick={() => setIsRejecting(true)}
                    className="px-4 py-2 border border-rose-200 text-rose-600 hover:bg-rose-50 dark:border-rose-800 dark:hover:bg-rose-950/30 rounded-xl text-xs font-bold transition-colors"
                  >
                    رفض الطلب
                  </button>
                  <button
                    type="button"
                    disabled={approveMutation.isPending}
                    onClick={handleApprove}
                    className="flex items-center gap-1.5 px-5 py-2 bg-primary-600 hover:bg-primary-700 text-white rounded-xl text-xs font-bold shadow-md transition-colors"
                  >
                    {approveMutation.isPending ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <CheckCircle2 className="w-4 h-4" />
                    )}
                    <span>اعتماد الطلب والكميات</span>
                  </button>
                </>
              )}

              {order.status === 'Approved' && !isInvoiceConfirmOpen && (
                <button
                  type="button"
                  onClick={() => setIsInvoiceConfirmOpen(true)}
                  className="flex items-center gap-1.5 px-5 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold shadow-md transition-colors"
                >
                  <FileText className="w-4 h-4" />
                  <span>تحويل إلى فاتورة مبيعات وخصم المخزون</span>
                </button>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('B2BOrderDetailsModal.tsx fixed and updated.');

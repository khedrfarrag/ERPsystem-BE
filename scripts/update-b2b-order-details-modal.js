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
  Package,
  Wand2,
  Coins,
  Tag,
  Boxes,
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

  // Local adjustments state: { [itemId]: { approvedQuantity: number, unitWholesalePrice: number, adjustmentReason: string } }
  const [adjustments, setAdjustments] = useState<
    Record<string, { approvedQuantity: number; unitWholesalePrice: number; adjustmentReason: string }>
  >({});

  // Reject state
  const [isRejecting, setIsRejecting] = useState(false);
  const [rejectionReason, setRejectionReason] = useState('');

  // Invoice state
  const [isInvoiceConfirmOpen, setIsInvoiceConfirmOpen] = useState(false);
  const [cashPaidAmount, setCashPaidAmount] = useState<number>(0);
  const [invoiceDiscount, setInvoiceDiscount] = useState<number>(0);
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
      const initial: Record<string, { approvedQuantity: number; unitWholesalePrice: number; adjustmentReason: string }> = {};
      order.items.forEach((item) => {
        initial[item.id] = {
          approvedQuantity: item.approvedQuantity ?? item.requestedQuantity,
          unitWholesalePrice: item.unitWholesalePrice,
          adjustmentReason: item.adjustmentReason || '',
        };
      });
      setAdjustments(initial);
      setIsRejecting(false);
      setRejectionReason('');
      setIsInvoiceConfirmOpen(false);
      setCashPaidAmount(order.expectedDownPayment || 0);
      setInvoiceDiscount(order.discountAmount || 0);
      setInvoiceNotes('');
      setCreditBreach(null);
      setCreditOverrideConfirmed(false);
      setCreditOverrideReason('');
    }
  }, [order]);

  if (!isOpen) return null;

  const isPending = order?.status === 'Pending';
  const isApproved = order?.status === 'Approved';

  const handleQtyChange = (itemId: string, val: number) => {
    setAdjustments((prev) => ({
      ...prev,
      [itemId]: {
        ...prev[itemId],
        approvedQuantity: Math.max(0, val),
      },
    }));
  };

  const handlePriceChange = (itemId: string, val: number) => {
    setAdjustments((prev) => ({
      ...prev,
      [itemId]: {
        ...prev[itemId],
        unitWholesalePrice: Math.max(0, val),
      },
    }));
  };

  const handleReasonChange = (itemId: string, reason: string) => {
    setAdjustments((prev) => ({
      ...prev,
      [itemId]: {
        ...prev[itemId],
        adjustmentReason: reason,
      },
    }));
  };

  // Assist: Auto-zero out of stock or deficient items
  const handleAutoZeroDeficits = () => {
    if (!order) return;
    let adjustedCount = 0;
    const nextAdjustments = { ...adjustments };

    order.items.forEach((item) => {
      const available = item.availableStock ?? 0;
      const currentApproved = nextAdjustments[item.id]?.approvedQuantity ?? item.requestedQuantity;
      if (available <= 0) {
        nextAdjustments[item.id] = {
          ...nextAdjustments[item.id],
          approvedQuantity: 0,
          adjustmentReason: 'المخزون نفد حالياً من المستودع',
        };
        adjustedCount++;
      } else if (currentApproved > available) {
        nextAdjustments[item.id] = {
          ...nextAdjustments[item.id],
          approvedQuantity: available,
          adjustmentReason: \`تخفيض الكمية لتطابق المخزون المتاح (\${available})\`,
        };
        adjustedCount++;
      }
    });

    setAdjustments(nextAdjustments);
    if (adjustedCount > 0) {
      toast.success(\`تم ضبط كميات \${adjustedCount} صنف وفق المخزون المتاح تلقائياً\`);
    } else {
      toast('جميع الأصناف متوفرة في المخزون بالكميات المطلوبة.', { icon: 'ℹ️' });
    }
  };

  // Calculate live review total
  const calculatedReviewTotal = order
    ? order.items.reduce((sum, item) => {
        const adj = adjustments[item.id];
        const qty = adj ? adj.approvedQuantity : item.approvedQuantity ?? item.requestedQuantity;
        const price = adj ? adj.unitWholesalePrice : item.unitWholesalePrice;
        return sum + qty * price;
      }, 0)
    : 0;

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
        unitWholesalePrice: adjustments[item.id]?.unitWholesalePrice ?? item.unitWholesalePrice,
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

  // Dynamic invoice calculations
  const effectiveInvoiceTotal = Math.max(0, (order?.totalAmount || 0) - invoiceDiscount);
  const actualCashPaid = Math.min(cashPaidAmount, effectiveInvoiceTotal);
  const changeDue = Math.max(0, cashPaidAmount - effectiveInvoiceTotal);
  const newDebt = Math.max(0, effectiveInvoiceTotal - cashPaidAmount);

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
      actualCashPaid >= effectiveInvoiceTotal
        ? 'Cash'
        : actualCashPaid > 0
        ? 'Mixed'
        : 'Credit';

    const payload = {
      paidAmount: actualCashPaid,
      discountAmount: invoiceDiscount,
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
        order.merchantTradeName || merchant?.tradeName || 'التاجر العزيز'
      )
    : null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="bg-white dark:bg-slate-800 rounded-2xl shadow-2xl border border-slate-200 dark:border-slate-700 w-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden">
        {/* Modal Header */}
        <div className="px-6 py-4 border-b border-slate-200 dark:border-slate-700 flex items-center justify-between bg-slate-50/50 dark:bg-slate-800/50">
          <div className="flex items-center gap-3">
            <div className="p-2.5 bg-primary-50 dark:bg-primary-950/50 text-primary-600 dark:text-primary-400 rounded-xl">
              <Receipt className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-lg font-black text-slate-900 dark:text-white font-mono">
                  {order ? \`#\${order.orderNumber}\` : 'تفاصيل طلب التوريد'}
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

                  {order.expectedDownPayment && order.expectedDownPayment > 0 && (
                    <div className="flex items-center justify-between text-xs p-1.5 bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-800/60 rounded-lg">
                      <span className="text-emerald-700 dark:text-emerald-300 font-semibold">دفعة مقدمة مقترحة من التاجر:</span>
                      <span className="font-mono font-bold text-emerald-700 dark:text-emerald-300">
                        {order.expectedDownPayment.toLocaleString('ar-EG')} ج.م
                      </span>
                    </div>
                  )}

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
                    <span>تم إنشاء فاتورة مبيعات رسمية وخصم المخزون بنجاح</span>
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

              {/* Order Items Table & Review Tools */}
              <div>
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 mb-3">
                  <h4 className="font-bold text-slate-800 dark:text-slate-200 text-sm flex items-center gap-2">
                    <Boxes className="w-4 h-4 text-primary-500" />
                    <span>أصناف الطلب والمراجعة</span>
                  </h4>

                  {isPending && (
                    <button
                      type="button"
                      onClick={handleAutoZeroDeficits}
                      className="flex items-center gap-1.5 px-3 py-1.5 bg-amber-500/10 hover:bg-amber-500/20 text-amber-700 dark:text-amber-300 border border-amber-500/30 rounded-xl text-xs font-bold transition-all"
                      title="يضبط كميات الأصناف التي يقل مخزونها عن المطلوب لتطابق المتاح أو تتصفر تلقائياً مع كتابة السبب"
                    >
                      <Wand2 className="w-3.5 h-3.5 text-amber-600" />
                      <span>تصفير وضبط الأصناف الناقصة تلقائياً</span>
                    </button>
                  )}
                </div>

                <div className="border border-slate-200 dark:border-slate-700 rounded-xl overflow-x-auto shadow-sm">
                  <table className="w-full text-right text-xs">
                    <thead className="bg-slate-50 dark:bg-slate-900/60 text-slate-500 border-b border-slate-200 dark:border-slate-700 font-semibold">
                      <tr>
                        <th className="px-3 py-3">الصنف</th>
                        <th className="px-3 py-3 text-center">المخزون بالمستودع</th>
                        <th className="px-3 py-3 text-center">سعر التكلفة</th>
                        <th className="px-3 py-3 text-center">سعر الجملة (ج.م)</th>
                        <th className="px-3 py-3 text-center">الكمية (مطلوبة / معتمدة)</th>
                        <th className="px-3 py-3 text-left">الإجمالي</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 dark:divide-slate-700/60">
                      {order.items.map((item) => {
                        const adj = adjustments[item.id] || {
                          approvedQuantity: item.approvedQuantity ?? item.requestedQuantity,
                          unitWholesalePrice: item.unitWholesalePrice,
                          adjustmentReason: item.adjustmentReason || '',
                        };

                        const isReduced = adj.approvedQuantity < item.requestedQuantity;
                        const linePrice = isPending ? adj.unitWholesalePrice : item.unitWholesalePrice;
                        const lineQty = isPending ? adj.approvedQuantity : item.approvedQuantity ?? item.requestedQuantity;
                        const lineTotal = lineQty * linePrice;
                        const stock = item.availableStock ?? 0;
                        const isDeficient = stock < item.requestedQuantity;

                        return (
                          <React.Fragment key={item.id}>
                            <tr
                              className={\`transition-colors \${
                                isReduced
                                  ? 'bg-amber-50/30 dark:bg-amber-950/10'
                                  : 'hover:bg-slate-50/50 dark:hover:bg-slate-800/40'
                              }\`}
                            >
                              <td className="px-3 py-3">
                                <div className="font-bold text-slate-800 dark:text-slate-100">
                                  {item.productName}
                                </div>
                              </td>

                              {/* Available Stock */}
                              <td className="px-3 py-3 text-center">
                                <span
                                  className={\`inline-flex items-center px-2 py-0.5 rounded-md font-mono font-bold text-[11px] \${
                                    stock <= 0
                                      ? 'bg-rose-100 text-rose-800 dark:bg-rose-950/60 dark:text-rose-300 border border-rose-300 dark:border-rose-800'
                                      : isDeficient
                                      ? 'bg-amber-100 text-amber-800 dark:bg-amber-950/60 dark:text-amber-300 border border-amber-300 dark:border-amber-800'
                                      : 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950/60 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800'
                                  }\`}
                                >
                                  {stock <= 0 ? 'نفد (0)' : stock}
                                </span>
                              </td>

                              {/* Purchase Cost */}
                              <td className="px-3 py-3 text-center font-mono text-slate-500 dark:text-slate-400">
                                {item.purchaseCost != null ? (
                                  <span>{item.purchaseCost.toLocaleString('ar-EG', { minimumFractionDigits: 2 })}</span>
                                ) : (
                                  <span className="text-slate-300 dark:text-slate-600">-</span>
                                )}
                              </td>

                              {/* Wholesale Price */}
                              <td className="px-3 py-3 text-center">
                                {isPending ? (
                                  <input
                                    type="number"
                                    min="0"
                                    step="0.5"
                                    value={adj.unitWholesalePrice}
                                    onChange={(e) => handlePriceChange(item.id, parseFloat(e.target.value) || 0)}
                                    className="w-20 px-2 py-1 text-center font-mono font-bold rounded-lg border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-white"
                                  />
                                ) : (
                                  <span className="font-mono text-slate-700 dark:text-slate-300">
                                    {item.unitWholesalePrice.toLocaleString('ar-EG', { minimumFractionDigits: 2 })}
                                  </span>
                                )}
                              </td>

                              {/* Quantities */}
                              <td className="px-3 py-3 text-center">
                                {isPending ? (
                                  <div className="flex items-center justify-center gap-1.5">
                                    <span className="text-slate-400 line-through text-[11px] font-mono">
                                      {item.requestedQuantity}
                                    </span>
                                    <span className="text-slate-400">←</span>
                                    <input
                                      type="number"
                                      min="0"
                                      max={item.requestedQuantity}
                                      value={adj.approvedQuantity}
                                      onChange={(e) =>
                                        handleQtyChange(
                                          item.id,
                                          Math.min(item.requestedQuantity, parseInt(e.target.value) || 0)
                                        )
                                      }
                                      className={\`w-16 px-2 py-1 text-center font-mono font-bold rounded-lg border \${
                                        isReduced
                                          ? 'border-amber-400 bg-amber-50 dark:bg-amber-950/40 text-amber-900 dark:text-amber-200'
                                          : 'border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-white'
                                      }\`}
                                    />
                                  </div>
                                ) : (
                                  <div className="font-mono font-bold">
                                    <span className={isReduced ? 'text-amber-600 dark:text-amber-400' : ''}>
                                      {item.approvedQuantity ?? item.requestedQuantity}
                                    </span>
                                    {isReduced && (
                                      <span className="text-[10px] text-slate-400 mr-1">
                                        (من أصل {item.requestedQuantity})
                                      </span>
                                    )}
                                  </div>
                                )}
                              </td>

                              <td className="px-3 py-3 text-left font-mono font-bold text-slate-900 dark:text-white">
                                {lineTotal.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                              </td>
                            </tr>

                            {/* Adjustment Reason row if reduced or recorded */}
                            {(isReduced || item.adjustmentReason) && (
                              <tr className="bg-amber-50/50 dark:bg-amber-950/20">
                                <td colSpan={6} className="px-3 py-2 text-xs">
                                  <div className="flex items-center gap-2 text-amber-800 dark:text-amber-300">
                                    <AlertTriangle className="w-3.5 h-3.5 flex-shrink-0" />
                                    <span className="font-semibold">
                                      سبب التعديل (المطلوب {item.requestedQuantity} ← المعتمد {adj.approvedQuantity}):
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
                        <td colSpan={5} className="px-3 py-3 text-left text-slate-700 dark:text-slate-300">
                          {isPending ? 'إجمالي القيمة المقترحة بعد التعديل:' : 'إجمالي قيمة الطلب المعتمدة:'}
                        </td>
                        <td className="px-3 py-3 text-left text-primary-600 dark:text-primary-400 text-base font-mono">
                          {(isPending ? calculatedReviewTotal : order.totalAmount).toLocaleString('ar-EG', {
                            minimumFractionDigits: 2,
                          })}{' '}
                          ج.م
                        </td>
                      </tr>
                    </tfoot>
                  </table>
                </div>
              </div>

              {/* Invoice Conversion Confirmation & Dynamic Change Due */}
              {isInvoiceConfirmOpen && (
                <div className="p-5 bg-primary-50/80 dark:bg-slate-900 border border-primary-300 dark:border-primary-800 rounded-2xl space-y-4 shadow-sm">
                  <div className="flex items-center justify-between">
                    <h4 className="font-bold text-primary-950 dark:text-primary-100 text-sm flex items-center gap-2">
                      <FileText className="w-4 h-4 text-primary-600" />
                      <span>تأكيد تحويل الطلب إلى فاتورة مبيعات وخصم المخزون</span>
                    </h4>
                    <button
                      type="button"
                      onClick={() => setIsInvoiceConfirmOpen(false)}
                      className="text-xs text-slate-400 hover:text-slate-600"
                    >
                      إلغاء
                    </button>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    {/* Invoice Discount */}
                    <div>
                      <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                        خصم تجاري على الفاتورة (ج.م):
                      </label>
                      <input
                        type="number"
                        min="0"
                        max={order.totalAmount}
                        value={invoiceDiscount}
                        onChange={(e) => setInvoiceDiscount(Math.max(0, parseFloat(e.target.value) || 0))}
                        className="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm font-mono font-bold"
                      />
                      <p className="text-[10px] text-slate-500 mt-1">
                        إجمالي بعد الخصم:{' '}
                        <strong className="text-slate-800 dark:text-slate-200 font-mono">
                          {effectiveInvoiceTotal.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                        </strong>
                      </p>
                    </div>

                    {/* Cash Received (allows overpayment for change calculation) */}
                    <div>
                      <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                        المبلغ المستلم نقداً (ج.م):
                      </label>
                      <input
                        type="number"
                        min="0"
                        value={cashPaidAmount}
                        onChange={(e) => setCashPaidAmount(Math.max(0, parseFloat(e.target.value) || 0))}
                        className="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-sm font-mono font-bold text-emerald-600 dark:text-emerald-400"
                      />
                      <p className="text-[10px] text-slate-500 mt-1">
                        المبلغ المسجل بالخزينة: {actualCashPaid.toLocaleString('ar-EG')} ج.م
                      </p>
                    </div>

                    {/* Invoice Notes */}
                    <div>
                      <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                        ملاحظات الفاتورة:
                      </label>
                      <input
                        type="text"
                        placeholder="أي تفاصيل تسليم أو شحن..."
                        value={invoiceNotes}
                        onChange={(e) => setInvoiceNotes(e.target.value)}
                        className="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800 text-xs"
                      />
                    </div>
                  </div>

                  {/* Dynamic Cash Change (الفكة) or Remaining Credit Due Card */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 p-3 rounded-xl bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700">
                    <div className="flex items-center gap-3">
                      <div className="p-2 rounded-xl bg-emerald-50 dark:bg-emerald-950/50 text-emerald-600">
                        <Coins className="w-5 h-5" />
                      </div>
                      <div>
                        <span className="text-[11px] text-slate-500 block">الباقي للعميل (الفكة المستردة)</span>
                        <span
                          className={\`font-mono font-extrabold text-base \${
                            changeDue > 0 ? 'text-emerald-600 dark:text-emerald-400 animate-pulse' : 'text-slate-400'
                          }\`}
                        >
                          {changeDue.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                        </span>
                      </div>
                    </div>

                    <div className="flex items-center gap-3">
                      <div className="p-2 rounded-xl bg-blue-50 dark:bg-blue-950/50 text-blue-600">
                        <Tag className="w-5 h-5" />
                      </div>
                      <div>
                        <span className="text-[11px] text-slate-500 block">المبلغ الآجل المتبقي على الحساب</span>
                        <span className="font-mono font-extrabold text-base text-slate-800 dark:text-slate-200">
                          {newDebt.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                        </span>
                      </div>
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
console.log('✓ B2BOrderDetailsModal.tsx updated successfully.');

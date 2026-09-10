const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/pages/portal/PortalOrders.tsx');

const content = `import React, { useState } from 'react';
import { useB2BOrders, useCancelB2BOrder } from '../../features/b2b/hooks/useB2BOrders';
import { useNavigate } from 'react-router-dom';
import {
  ShoppingBag,
  Clock,
  CheckCircle2,
  FileText,
  XCircle,
  ChevronDown,
  ChevronUp,
  RotateCcw,
  Ban,
  Loader2,
  AlertCircle,
  Receipt,
} from 'lucide-react';
import toast from 'react-hot-toast';

export const PortalOrders: React.FC = () => {
  const navigate = useNavigate();
  const { data, isLoading } = useB2BOrders({ page: 1, pageSize: 50 });
  const cancelMutation = useCancelB2BOrder();

  const [expandedOrderId, setExpandedOrderId] = useState<string | null>(null);
  const [cancellingOrder, setCancellingOrder] = useState<string | null>(null);
  const [cancelReason, setCancelReason] = useState('');

  const orders = data?.items || [];

  const toggleExpand = (id: string) => {
    setExpandedOrderId((prev) => (prev === id ? null : id));
  };

  const handleOpenCancelModal = (orderId: string) => {
    setCancellingOrder(orderId);
    setCancelReason('');
  };

  const handleConfirmCancel = async () => {
    if (!cancellingOrder) return;
    try {
      await cancelMutation.mutateAsync({
        id: cancellingOrder,
        reason: cancelReason.trim() || undefined,
      });
      setCancellingOrder(null);
    } catch {
      // Handled in hook
    }
  };

  const handleReorder = (order: (typeof orders)[0]) => {
    try {
      // Format items for cart
      const cartPayload = order.items.map((it) => ({
        product: {
          id: it.productId,
          name: it.productName,
          barcode: null,
          unitName: null,
          categoryName: null,
          availableStock: 999,
          effectiveWholesalePrice: it.unitWholesalePrice,
        },
        quantity: it.approvedQuantity ?? it.requestedQuantity,
      }));

      localStorage.setItem('b2b_merchant_cart', JSON.stringify(cartPayload));
      toast.success('تم نسخ أصناف الطلب إلى سلة المشتريات بنجاح!');
      navigate('/portal/catalog');
    } catch {
      toast.error('حدث خطأ أثناء نسخ الأصناف');
    }
  };

  return (
    <div className="space-y-6 pb-20 max-w-5xl mx-auto animate-in fade-in duration-200">
      {/* Page Header */}
      <div>
        <h1 className="text-2xl font-black text-slate-900 dark:text-white flex items-center gap-2.5">
          <ShoppingBag className="w-7 h-7 text-emerald-600 dark:text-emerald-400" />
          <span>سجل طلبات التوريد الخاصة بي</span>
        </h1>
        <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">
          متابعة حالة طلبات التوريد المرسلة، تفاصيل الكميات المعتمدة، وإعادة طلب نفس الأصناف
        </p>
      </div>

      {/* Orders List */}
      {isLoading ? (
        <div className="flex flex-col items-center justify-center py-16 bg-white dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700">
          <Loader2 className="w-8 h-8 text-emerald-600 animate-spin mb-2" />
          <p className="text-sm text-slate-500">جاري تحميل سجل الطلبات...</p>
        </div>
      ) : orders.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 bg-white dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700 text-center p-6">
          <ShoppingBag className="w-12 h-12 text-slate-300 dark:text-slate-600 mb-3" />
          <h3 className="text-base font-bold text-slate-800 dark:text-slate-200">
            لم تقم بإنشاء أي طلبات توريد بعد
          </h3>
          <p className="text-xs text-slate-400 mt-1 max-w-md">
            يمكنك تصفح كتالوج المنتجات المتاحة بأسعار الجملة وإرسال أول طلب توريد لمتجرك بسهولة.
          </p>
          <button
            type="button"
            onClick={() => navigate('/portal/catalog')}
            className="mt-4 px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-xs font-bold shadow-md shadow-emerald-600/20 transition-colors"
          >
            تصفح الكتالوج وبدء الشراء
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {orders.map((order) => {
            const isExpanded = expandedOrderId === order.id;
            const canCancel = order.status === 'Pending';

            return (
              <div
                key={order.id}
                className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200/80 dark:border-slate-700 shadow-sm overflow-hidden transition-all"
              >
                {/* Order Summary Row */}
                <div className="p-5 flex flex-col md:flex-row md:items-center justify-between gap-4">
                  <div className="flex items-start gap-4">
                    <div className="p-3 rounded-2xl bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400">
                      <Receipt className="w-6 h-6" />
                    </div>
                    <div>
                      <div className="flex items-center gap-3">
                        <span className="font-mono font-black text-base text-slate-900 dark:text-white">
                          #{order.orderNumber}
                        </span>
                        <span
                          className={\`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-bold \${
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
                          {order.status === 'Pending' && <Clock className="w-3 h-3" />}
                          {order.status === 'Approved' && <CheckCircle2 className="w-3 h-3" />}
                          {order.status === 'Invoiced' && <FileText className="w-3 h-3" />}
                          {order.status === 'Rejected' && <XCircle className="w-3 h-3" />}
                          <span>
                            {order.status === 'Pending'
                              ? 'قيد المراجعة'
                              : order.status === 'Approved'
                              ? 'معتمد للتوريد'
                              : order.status === 'Invoiced'
                              ? 'تمت الفوترة والتجهيز'
                              : order.status === 'Rejected'
                              ? 'مرفوض'
                              : 'ملغي'}
                          </span>
                        </span>
                      </div>
                      <div className="flex flex-wrap items-center gap-3 text-xs text-slate-500 dark:text-slate-400 mt-1">
                        <span>{new Date(order.createdAt).toLocaleString('ar-EG')}</span>
                        <span>•</span>
                        <span>{order.items.length} صنف</span>
                        <span>•</span>
                        <span>
                          السداد:{' '}
                          {order.paymentPreference === 'Cash'
                            ? 'نقدي'
                            : order.paymentPreference === 'Credit'
                            ? 'آجل'
                            : 'جزئي'}
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Pricing & Actions */}
                  <div className="flex items-center justify-between md:justify-end gap-3 pt-3 md:pt-0 border-t md:border-t-0 border-slate-100 dark:border-slate-700/60">
                    <div className="text-right">
                      <div className="text-[10px] text-slate-400">إجمالي الطلب</div>
                      <div className="font-mono font-black text-base text-emerald-600 dark:text-emerald-400">
                        {order.totalAmount.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                      </div>
                    </div>

                    <div className="flex items-center gap-2">
                      {/* Cancel Order (Pending only) */}
                      {canCancel && (
                        <button
                          type="button"
                          onClick={() => handleOpenCancelModal(order.id)}
                          title="إلغاء الطلب"
                          className="px-3 py-2 border border-rose-200 dark:border-rose-800 text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/30 rounded-xl text-xs font-bold transition-colors flex items-center gap-1.5"
                        >
                          <Ban className="w-3.5 h-3.5" />
                          <span>إلغاء الطلب</span>
                        </button>
                      )}

                      {/* Reorder Button */}
                      <button
                        type="button"
                        onClick={() => handleReorder(order)}
                        title="نسخ الأصناف إلى السلة"
                        className="px-3 py-2 bg-slate-100 dark:bg-slate-700 hover:bg-slate-200 dark:hover:bg-slate-600 text-slate-800 dark:text-slate-200 rounded-xl text-xs font-bold transition-colors flex items-center gap-1.5"
                      >
                        <RotateCcw className="w-3.5 h-3.5" />
                        <span>طلب مماثل</span>
                      </button>

                      {/* Expand / Collapse Button */}
                      <button
                        type="button"
                        onClick={() => toggleExpand(order.id)}
                        className="p-2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded-xl transition-colors"
                      >
                        {isExpanded ? (
                          <ChevronUp className="w-5 h-5" />
                        ) : (
                          <ChevronDown className="w-5 h-5" />
                        )}
                      </button>
                    </div>
                  </div>
                </div>

                {/* Expandable Order Details Drawer */}
                {isExpanded && (
                  <div className="px-5 pb-5 pt-2 border-t border-slate-100 dark:border-slate-700/60 bg-slate-50/50 dark:bg-slate-900/20 space-y-4">
                    {/* Status Note If Rejected or Cancelled */}
                    {order.status === 'Rejected' && order.rejectionReason && (
                      <div className="p-3 bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 rounded-xl text-rose-800 dark:text-rose-300 text-xs">
                        <span className="font-bold">سبب رفض الطلب من الإدارة: </span>
                        <span>{order.rejectionReason}</span>
                      </div>
                    )}

                    {order.status === 'Cancelled' && order.cancellationReason && (
                      <div className="p-3 bg-amber-50 dark:bg-amber-950/40 border border-amber-200 dark:border-amber-800 rounded-xl text-amber-800 dark:text-amber-300 text-xs">
                        <span className="font-bold">سبب الإلغاء: </span>
                        <span>{order.cancellationReason}</span>
                      </div>
                    )}

                    {/* Line Items Table */}
                    <div className="border border-slate-200 dark:border-slate-700 rounded-xl overflow-hidden bg-white dark:bg-slate-800">
                      <table className="w-full text-right text-xs">
                        <thead className="bg-slate-100 dark:bg-slate-700/60 text-slate-600 dark:text-slate-300 font-bold">
                          <tr>
                            <th className="px-3 py-2.5">الصنف</th>
                            <th className="px-3 py-2.5 text-center">الكمية المطلوبة</th>
                            <th className="px-3 py-2.5 text-center">سعر الوحدة</th>
                            <th className="px-3 py-2.5 text-center">الكمية المعتمدة</th>
                            <th className="px-3 py-2.5 text-left">الإجمالي</th>
                          </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 dark:divide-slate-700/60">
                          {order.items.map((item) => {
                            const approved = item.approvedQuantity ?? item.requestedQuantity;
                            const isReduced = approved < item.requestedQuantity;
                            const subtotal = approved * item.unitWholesalePrice;

                            return (
                              <React.Fragment key={item.id}>
                                <tr>
                                  <td className="px-3 py-2.5 font-bold text-slate-800 dark:text-slate-200">
                                    {item.productName}
                                  </td>
                                  <td className="px-3 py-2.5 text-center text-slate-600 dark:text-slate-400">
                                    {item.requestedQuantity}
                                  </td>
                                  <td className="px-3 py-2.5 text-center text-slate-600 dark:text-slate-400">
                                    {item.unitWholesalePrice.toLocaleString('ar-EG')} ج.م
                                  </td>
                                  <td className="px-3 py-2.5 text-center font-bold">
                                    <span
                                      className={
                                        isReduced
                                          ? 'text-amber-600 dark:text-amber-400 font-black'
                                          : 'text-slate-900 dark:text-white'
                                      }
                                    >
                                      {approved}
                                    </span>
                                  </td>
                                  <td className="px-3 py-2.5 text-left font-bold text-slate-900 dark:text-white">
                                    {subtotal.toLocaleString('ar-EG')} ج.م
                                  </td>
                                </tr>

                                {item.adjustmentReason && (
                                  <tr className="bg-amber-50/50 dark:bg-amber-950/20">
                                    <td colSpan={5} className="px-3 py-1.5 text-[11px] text-amber-800 dark:text-amber-300">
                                      ملاحظة تعديل الكمية من الإدارة: {item.adjustmentReason}
                                    </td>
                                  </tr>
                                )}
                              </React.Fragment>
                            );
                          })}
                        </tbody>
                      </table>
                    </div>

                    {/* Delivery Notes If any */}
                    {order.notes && (
                      <div className="text-xs text-slate-500 dark:text-slate-400">
                        <span className="font-semibold text-slate-700 dark:text-slate-300">
                          ملاحظات التوصيل:
                        </span>{' '}
                        {order.notes}
                      </div>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Cancellation Modal */}
      {cancellingOrder && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
          <div className="bg-white dark:bg-slate-800 rounded-2xl max-w-md w-full p-6 border border-slate-200 dark:border-slate-700 shadow-2xl space-y-4 animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-center gap-3 text-rose-600">
              <AlertCircle className="w-6 h-6" />
              <h3 className="font-bold text-base text-slate-900 dark:text-white">
                تأكيد إلغاء طلب التوريد
              </h3>
            </div>

            <p className="text-xs text-slate-500 leading-relaxed">
              هل أنت متأكد من رغبتك في إلغاء هذا الطلب؟ لا يمكن التراجع عن الإلغاء بمجرد تأكيده،
              وإذا كنت ترغب بتعديل الأصناف يمكنك إلغاء هذا الطلب وإنشاء طلب جديد.
            </p>

            <div>
              <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                سبب الإلغاء (اختياري):
              </label>
              <textarea
                rows={2}
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                placeholder="مثال: الرغبة في تعديل الكميات أو خطأ في تحديد الصنف..."
                className="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-600 bg-slate-50 dark:bg-slate-900 text-xs"
              />
            </div>

            <div className="flex justify-end gap-2 pt-2">
              <button
                type="button"
                onClick={() => setCancellingOrder(null)}
                className="px-4 py-2 text-xs font-bold text-slate-600 dark:text-slate-400 hover:bg-slate-100 rounded-xl"
              >
                تراجع
              </button>
              <button
                type="button"
                disabled={cancelMutation.isPending}
                onClick={handleConfirmCancel}
                className="px-4 py-2 bg-rose-600 hover:bg-rose-700 text-white rounded-xl text-xs font-bold shadow-md transition-colors"
              >
                {cancelMutation.isPending ? 'جاري الإلغاء...' : 'تأكيد الإلغاء'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default PortalOrders;
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('PortalOrders.tsx created successfully.');

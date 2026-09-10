const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/pages/B2BOrders.tsx');

const content = `import React, { useState } from 'react';
import { useB2BOrders } from '../features/b2b/hooks/useB2BOrders';
import { B2BOrderDetailsModal } from '../features/b2b/components/B2BOrderDetailsModal';
import { buildOrderWhatsAppUrl } from '../features/b2b/utils/whatsappUtils';
import {
  ShoppingBag,
  Search,
  Eye,
  MessageCircle,
  Clock,
  CheckCircle2,
  FileText,
  XCircle,
  RefreshCw,
} from 'lucide-react';

const STATUS_TABS = [
  { key: 'ALL', label: 'كافة الطلبات' },
  { key: 'Pending', label: 'قيد المراجعة' },
  { key: 'Approved', label: 'معتمدة للتوريد' },
  { key: 'Invoiced', label: 'تمت الفوترة' },
  { key: 'Rejected', label: 'مرفوضة' },
  { key: 'Cancelled', label: 'ملغاة' },
];

export const B2BOrders: React.FC = () => {
  const [activeTab, setActiveTab] = useState('ALL');
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);
  const [isDetailsOpen, setIsDetailsOpen] = useState(false);
  const [page, setPage] = useState(1);

  const statusFilter = activeTab === 'ALL' ? undefined : activeTab;
  const { data, isLoading, refetch, isFetching } = useB2BOrders({
    page,
    pageSize: 15,
    status: statusFilter,
  });

  const orders = data?.items || [];

  // Filter client-side by search if any
  const filteredOrders = orders.filter((order) => {
    if (!searchTerm.trim()) return true;
    const term = searchTerm.toLowerCase();
    return (
      order.orderNumber.toLowerCase().includes(term) ||
      (order.merchantTradeName && order.merchantTradeName.toLowerCase().includes(term)) ||
      (order.merchantPhone && order.merchantPhone.includes(term))
    );
  });

  const handleOpenDetails = (id: string) => {
    setSelectedOrderId(id);
    setIsDetailsOpen(true);
  };

  return (
    <div className="space-y-6 pb-12 animate-in fade-in duration-200">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-slate-900 dark:text-white flex items-center gap-2.5">
            <ShoppingBag className="w-7 h-7 text-primary-600 dark:text-primary-400" />
            <span>طلبات الجملة B2B</span>
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">
            متابعة طلبات التوريد الواردة من التجار المعتمدين، اعتماد الكميات، والتحويل لفواتير مبيعات
          </p>
        </div>

        <button
          type="button"
          onClick={() => refetch()}
          disabled={isFetching}
          className="flex items-center gap-2 px-3.5 py-2 rounded-xl bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-200 text-xs font-bold hover:bg-slate-50 dark:hover:bg-slate-700/50 shadow-sm transition-colors self-start sm:self-auto"
        >
          <RefreshCw className={\`w-4 h-4 \${isFetching ? 'animate-spin text-primary-600' : ''}\`} />
          <span>تحديث القائمة</span>
        </button>
      </div>

      {/* Tabs & Search Bar */}
      <div className="bg-white dark:bg-slate-800 p-4 rounded-2xl border border-slate-200/80 dark:border-slate-700 shadow-sm space-y-4">
        <div className="flex flex-wrap items-center gap-2 border-b border-slate-100 dark:border-slate-700/60 pb-3">
          {STATUS_TABS.map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => {
                setActiveTab(tab.key);
                setPage(1);
              }}
              className={\`px-3.5 py-1.5 rounded-xl text-xs font-bold transition-all \${
                activeTab === tab.key
                  ? 'bg-primary-600 text-white shadow-md shadow-primary-600/20'
                  : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-700/50'
              }\`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="relative">
          <Search className="w-4 h-4 absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            placeholder="بحث برقم الطلب، اسم التاجر، أو رقم الهاتف..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-4 pr-10 py-2.5 bg-slate-50 dark:bg-slate-900/50 border border-slate-200 dark:border-slate-700 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all placeholder:text-slate-400 text-slate-900 dark:text-white"
          />
        </div>
      </div>

      {/* Orders Table */}
      <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200/80 dark:border-slate-700 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-right text-sm">
            <thead className="bg-slate-50 dark:bg-slate-900/40 text-slate-600 dark:text-slate-400 text-xs font-bold border-b border-slate-200 dark:border-slate-700">
              <tr>
                <th className="px-5 py-3.5">رقم الطلب</th>
                <th className="px-4 py-3.5">التاجر</th>
                <th className="px-4 py-3.5 text-center">التاريخ</th>
                <th className="px-4 py-3.5 text-center">عدد الأصناف</th>
                <th className="px-4 py-3.5 text-center">القيمة الإجمالية</th>
                <th className="px-4 py-3.5 text-center">طريقة السداد</th>
                <th className="px-4 py-3.5 text-center">الحالة</th>
                <th className="px-5 py-3.5 text-left">الإجراءات</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-700/60">
              {isLoading ? (
                <tr>
                  <td colSpan={8} className="text-center py-12 text-slate-400 text-sm">
                    جاري تحميل طلبات الجملة...
                  </td>
                </tr>
              ) : filteredOrders.length === 0 ? (
                <tr>
                  <td colSpan={8} className="text-center py-12">
                    <div className="flex flex-col items-center justify-center text-slate-400">
                      <ShoppingBag className="w-12 h-12 stroke-[1.5] text-slate-300 dark:text-slate-600 mb-2" />
                      <p className="text-sm font-semibold">لا توجد طلبات توريد مطابقة</p>
                    </div>
                  </td>
                </tr>
              ) : (
                filteredOrders.map((order) => {
                  const waUrl = order.merchantPhone
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
                    <tr
                      key={order.id}
                      className="hover:bg-slate-50/70 dark:hover:bg-slate-700/30 transition-colors"
                    >
                      <td className="px-5 py-4 font-mono font-bold text-primary-600 dark:text-primary-400">
                        #{order.orderNumber}
                      </td>

                      <td className="px-4 py-4">
                        <div className="font-bold text-slate-900 dark:text-white">
                          {order.merchantTradeName || 'تاجر جملة'}
                        </div>
                        <div className="text-xs text-slate-500 dark:text-slate-400" dir="ltr">
                          {order.merchantPhone || ''}
                        </div>
                      </td>

                      <td className="px-4 py-4 text-center text-xs text-slate-600 dark:text-slate-400">
                        {new Date(order.createdAt).toLocaleDateString('ar-EG')}
                      </td>

                      <td className="px-4 py-4 text-center font-bold text-slate-700 dark:text-slate-300">
                        {order.items.length}
                      </td>

                      <td className="px-4 py-4 text-center font-bold text-slate-900 dark:text-white">
                        {order.totalAmount.toLocaleString('ar-EG')} ج.م
                      </td>

                      <td className="px-4 py-4 text-center">
                        <span className="text-xs px-2.5 py-1 bg-slate-100 dark:bg-slate-700 rounded-lg text-slate-700 dark:text-slate-300 font-medium">
                          {order.paymentPreference === 'Cash'
                            ? 'نقدي'
                            : order.paymentPreference === 'Credit'
                            ? 'آجل'
                            : 'جزئي'}
                        </span>
                      </td>

                      <td className="px-4 py-4 text-center">
                        <span
                          className={\`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold \${
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
                              ? 'تمت الفوترة'
                              : order.status === 'Rejected'
                              ? 'مرفوض'
                              : 'ملغي'}
                          </span>
                        </span>
                      </td>

                      <td className="px-5 py-4 text-left">
                        <div className="flex items-center justify-end gap-2">
                          {waUrl && (
                            <a
                              href={waUrl}
                              target="_blank"
                              rel="noopener noreferrer"
                              title="فتح محادثة واتساب جاهزة"
                              className="p-2 text-emerald-600 hover:bg-emerald-50 dark:hover:bg-emerald-950/40 rounded-xl transition-colors"
                            >
                              <MessageCircle className="w-4 h-4" />
                            </a>
                          )}
                          <button
                            type="button"
                            onClick={() => handleOpenDetails(order.id)}
                            className="flex items-center gap-1.5 px-3 py-1.5 bg-primary-50 dark:bg-primary-950/40 hover:bg-primary-100 text-primary-700 dark:text-primary-300 rounded-xl text-xs font-bold transition-colors"
                          >
                            <Eye className="w-3.5 h-3.5" />
                            <span>مراجعة وتدقيق</span>
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Order Details Modal */}
      <B2BOrderDetailsModal
        orderId={selectedOrderId}
        isOpen={isDetailsOpen}
        onClose={() => {
          setIsDetailsOpen(false);
          setSelectedOrderId(null);
        }}
      />
    </div>
  );
};

export default B2BOrders;
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('B2BOrders.tsx updated with correct types.');

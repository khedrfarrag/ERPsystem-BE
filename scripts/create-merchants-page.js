const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/pages/Merchants.tsx');

const content = `import React, { useState } from 'react';
import { 
  useMerchants, 
  useCreateMerchant, 
  useUpdateMerchant, 
  useToggleMerchantActive 
} from '../features/b2b/hooks/useMerchants';
import { MerchantsTable } from '../features/b2b/components/MerchantsTable';
import { CreateMerchantModal } from '../features/b2b/components/CreateMerchantModal';
import { Merchant } from '../features/b2b/types/b2b.types';
import { 
  Building2, 
  Plus, 
  Search, 
  Coins, 
  CreditCard, 
  AlertTriangle,
  ChevronRight,
  ChevronLeft
} from 'lucide-react';

export const Merchants: React.FC = () => {
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [merchantToEdit, setMerchantToEdit] = useState<Merchant | null>(null);

  // Queries & Mutations
  const { data, isLoading } = useMerchants({
    page,
    pageSize,
    search: search.trim() || undefined,
    isActive: statusFilter === 'active' ? true : statusFilter === 'inactive' ? false : undefined,
  });

  const createMutation = useCreateMerchant();
  const updateMutation = useUpdateMerchant();
  const toggleMutation = useToggleMerchantActive();

  const merchants = data?.items || [];
  const totalCount = data?.totalCount || 0;
  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  // Aggregate Metrics
  const totalOutstandingBalance = merchants.reduce((acc, m) => acc + (m.currentBalance || 0), 0);
  const totalCreditLimits = merchants.reduce((acc, m) => acc + (m.creditLimit || 0), 0);
  const overLimitCount = merchants.filter(m => m.creditLimit > 0 && m.currentBalance > m.creditLimit).length;

  const handleOpenCreate = () => {
    setMerchantToEdit(null);
    setIsModalOpen(true);
  };

  const handleOpenEdit = (merchant: Merchant) => {
    setMerchantToEdit(merchant);
    setIsModalOpen(true);
  };

  return (
    <div className="space-y-6 animate-fadeIn" dir="rtl">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-slate-900 border border-slate-800 p-6 rounded-3xl shadow-sm">
        <div className="flex items-center gap-4">
          <div className="w-12 h-12 rounded-2xl bg-gradient-to-tr from-primary-600 to-indigo-600 flex items-center justify-center text-white shadow-lg shadow-primary-500/20">
            <Building2 className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-white tracking-tight">عملاء الجملة والمتاجر (B2B)</h1>
            <p className="text-sm text-slate-400 mt-0.5">
              إدارة حسابات متاجر التجزئة الشريكة، سقوف الائتمان، وبيانات دخول بوابة التوريد.
            </p>
          </div>
        </div>

        <button
          onClick={handleOpenCreate}
          className="flex items-center justify-center gap-2 px-5 py-3 rounded-2xl bg-primary-600 hover:bg-primary-500 text-white font-semibold text-sm shadow-lg shadow-primary-600/30 transition-all hover:scale-[1.02] active:scale-[0.98]"
        >
          <Plus className="w-5 h-5" />
          <span>إضافة تاجر جملة جديد</span>
        </button>
      </div>

      {/* Metrics Summary Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-slate-900 border border-slate-800 p-5 rounded-2xl shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-400">إجمالي التجار المسجلين</span>
            <div className="w-8 h-8 rounded-xl bg-blue-500/10 text-blue-400 flex items-center justify-center">
              <Building2 className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3 text-2xl font-extrabold text-white font-mono">{totalCount}</div>
        </div>

        <div className="bg-slate-900 border border-slate-800 p-5 rounded-2xl shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-400">إجمالي الرصيد القائم (الآجل)</span>
            <div className="w-8 h-8 rounded-xl bg-amber-500/10 text-amber-400 flex items-center justify-center">
              <Coins className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3 text-2xl font-extrabold text-amber-400 font-mono">
            {totalOutstandingBalance.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} <span className="text-xs font-normal">ج.م</span>
          </div>
        </div>

        <div className="bg-slate-900 border border-slate-800 p-5 rounded-2xl shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-400">إجمالي سقوف الائتمان</span>
            <div className="w-8 h-8 rounded-xl bg-emerald-500/10 text-emerald-400 flex items-center justify-center">
              <CreditCard className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3 text-2xl font-extrabold text-white font-mono">
            {totalCreditLimits.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} <span className="text-xs font-normal">ج.م</span>
          </div>
        </div>

        <div className="bg-slate-900 border border-slate-800 p-5 rounded-2xl shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-400">تجاوزوا سقف الائتمان</span>
            <div className="w-8 h-8 rounded-xl bg-rose-500/10 text-rose-400 flex items-center justify-center">
              <AlertTriangle className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3 text-2xl font-extrabold text-rose-400 font-mono">{overLimitCount}</div>
        </div>
      </div>

      {/* Filter & Search Bar */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-4 bg-slate-900 border border-slate-800 p-4 rounded-2xl">
        <div className="relative w-full sm:w-80">
          <Search className="w-4 h-4 absolute right-3.5 top-3.5 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder="بحث بالاسم، المسئول، أو الهاتف..."
            className="w-full bg-slate-950 border border-slate-800 rounded-xl py-2.5 pr-10 pl-4 text-sm text-white placeholder:text-slate-500 focus:outline-none focus:border-primary-500 transition"
          />
        </div>

        <div className="flex items-center gap-2 w-full sm:w-auto">
          <button
            onClick={() => { setStatusFilter('all'); setPage(1); }}
            className={\`px-4 py-2 rounded-xl text-xs font-bold transition \${
              statusFilter === 'all'
                ? 'bg-primary-600 text-white shadow-sm'
                : 'bg-slate-800 text-slate-400 hover:text-white'
            }\`}
          >
            الكل ({totalCount})
          </button>
          <button
            onClick={() => { setStatusFilter('active'); setPage(1); }}
            className={\`px-4 py-2 rounded-xl text-xs font-bold transition \${
              statusFilter === 'active'
                ? 'bg-emerald-600 text-white shadow-sm'
                : 'bg-slate-800 text-slate-400 hover:text-white'
            }\`}
          >
            النشطين فقط
          </button>
          <button
            onClick={() => { setStatusFilter('inactive'); setPage(1); }}
            className={\`px-4 py-2 rounded-xl text-xs font-bold transition \${
              statusFilter === 'inactive'
                ? 'bg-rose-600 text-white shadow-sm'
                : 'bg-slate-800 text-slate-400 hover:text-white'
            }\`}
          >
            المعطلين
          </button>
        </div>
      </div>

      {/* Merchants Table */}
      <MerchantsTable
        merchants={merchants}
        isLoading={isLoading}
        onEdit={handleOpenEdit}
        onToggleActive={(id) => toggleMutation.mutate(id)}
      />

      {/* Pagination Bar */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between bg-slate-900 border border-slate-800 px-6 py-4 rounded-2xl text-xs text-slate-400">
          <div>
            عرض صفحة <span className="font-bold text-white">{page}</span> من <span className="font-bold text-white">{totalPages}</span> (إجمالي {totalCount} تاجر)
          </div>
          <div className="flex items-center gap-2">
            <button
              disabled={page <= 1}
              onClick={() => setPage(p => p - 1)}
              className="p-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-white disabled:opacity-30 disabled:cursor-not-allowed transition"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
            <button
              disabled={page >= totalPages}
              onClick={() => setPage(p => p + 1)}
              className="p-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-white disabled:opacity-30 disabled:cursor-not-allowed transition"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      {/* Create / Edit Modal */}
      <CreateMerchantModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onSubmitCreate={(payload) => createMutation.mutateAsync(payload)}
        onSubmitUpdate={(id, payload) => updateMutation.mutateAsync({ id, data: payload })}
        merchantToEdit={merchantToEdit}
      />
    </div>
  );
};

export default Merchants;
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('Merchants.tsx created successfully.');

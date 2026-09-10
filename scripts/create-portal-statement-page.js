const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/pages/portal/PortalStatement.tsx');

const content = `import React, { useState } from 'react';
import { useCurrentMerchant, useMyStatement } from '../../features/b2b/hooks/useMerchants';
import {
  FileText,
  CreditCard,
  Coins,
  TrendingUp,
  Calendar,
  Printer,
  Loader2,
  ArrowUpRight,
  ArrowDownLeft,
  Building2,
  Phone,
  ShieldCheck,
} from 'lucide-react';

export const PortalStatement: React.FC = () => {
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');

  const { data: merchant, isLoading: isMerchantLoading } = useCurrentMerchant();
  const { data: statement, isLoading: isStatementLoading } = useMyStatement({
    from: fromDate || undefined,
    to: toDate || undefined,
  });

  const transactions = statement?.transactions || [];

  const creditLimit = merchant?.creditLimit || 0;
  const currentBalance = merchant?.currentBalance || 0;
  const availableCredit = Math.max(0, creditLimit - currentBalance);
  const utilizationRate = creditLimit > 0 ? Math.min(100, (currentBalance / creditLimit) * 100) : 0;

  const handlePrint = () => {
    window.print();
  };

  return (
    <div className="space-y-6 pb-20 max-w-5xl mx-auto animate-in fade-in duration-200">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-slate-900 dark:text-white flex items-center gap-2.5">
            <FileText className="w-7 h-7 text-emerald-600 dark:text-emerald-400" />
            <span>كشف الحساب المالي</span>
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">
            متابعة رصيد المديونية، سقف الائتمان الممنوح، وحركة الفواتير والمدفوعات المسجلة
          </p>
        </div>

        <button
          type="button"
          onClick={handlePrint}
          className="flex items-center gap-2 px-4 py-2 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-200 rounded-xl text-xs font-bold hover:bg-slate-50 dark:hover:bg-slate-700/50 shadow-sm transition-colors self-start sm:self-auto print:hidden"
        >
          <Printer className="w-4 h-4" />
          <span>طباعة كشف الحساب</span>
        </button>
      </div>

      {/* Merchant Financial Snapshot Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {/* Credit Limit */}
        <div className="bg-white dark:bg-slate-800 p-5 rounded-2xl border border-slate-200/80 dark:border-slate-700 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-500 dark:text-slate-400">
              سقف الائتمان الممنوح
            </span>
            <div className="p-2 bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 rounded-xl">
              <CreditCard className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3">
            <div className="text-2xl font-black text-slate-900 dark:text-white font-mono">
              {creditLimit.toLocaleString('ar-EG', { minimumFractionDigits: 2 })}{' '}
              <span className="text-xs font-normal text-slate-500">ج.م</span>
            </div>
          </div>
        </div>

        {/* Current Balance */}
        <div className="bg-white dark:bg-slate-800 p-5 rounded-2xl border border-slate-200/80 dark:border-slate-700 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-500 dark:text-slate-400">
              الرصيد القائم الحالي
            </span>
            <div
              className={\`p-2 rounded-xl \${
                currentBalance > creditLimit
                  ? 'bg-rose-50 dark:bg-rose-950/40 text-rose-600'
                  : 'bg-amber-50 dark:bg-amber-950/40 text-amber-600'
              }\`}
            >
              <Coins className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3">
            <div
              className={\`text-2xl font-black font-mono \${
                currentBalance > creditLimit
                  ? 'text-rose-600'
                  : 'text-slate-900 dark:text-white'
              }\`}
            >
              {currentBalance.toLocaleString('ar-EG', { minimumFractionDigits: 2 })}{' '}
              <span className="text-xs font-normal text-slate-500">ج.م</span>
            </div>
          </div>
        </div>

        {/* Available Credit */}
        <div className="bg-white dark:bg-slate-800 p-5 rounded-2xl border border-slate-200/80 dark:border-slate-700 shadow-sm">
          <div className="flex items-center justify-between">
            <span className="text-xs font-semibold text-slate-500 dark:text-slate-400">
              الرصيد الائتماني المتاح
            </span>
            <div className="p-2 bg-blue-50 dark:bg-blue-950/40 text-blue-600 rounded-xl">
              <TrendingUp className="w-4 h-4" />
            </div>
          </div>
          <div className="mt-3">
            <div className="text-2xl font-black text-slate-900 dark:text-white font-mono">
              {availableCredit.toLocaleString('ar-EG', { minimumFractionDigits: 2 })}{' '}
              <span className="text-xs font-normal text-slate-500">ج.م</span>
            </div>
          </div>
        </div>
      </div>

      {/* Credit Utilization Bar */}
      {creditLimit > 0 && (
        <div className="bg-white dark:bg-slate-800 p-4 rounded-2xl border border-slate-200/80 dark:border-slate-700 shadow-sm space-y-2">
          <div className="flex items-center justify-between text-xs">
            <span className="font-semibold text-slate-700 dark:text-slate-300">
              نسبة استهلاك سقف الائتمان:
            </span>
            <span
              className={\`font-mono font-black \${
                utilizationRate >= 90 ? 'text-rose-600' : 'text-emerald-600'
              }\`}
            >
              {utilizationRate.toFixed(1)}%
            </span>
          </div>
          <div className="w-full h-2.5 bg-slate-100 dark:bg-slate-700 rounded-full overflow-hidden">
            <div
              className={\`h-full rounded-full transition-all duration-500 \${
                utilizationRate >= 90
                  ? 'bg-rose-500'
                  : utilizationRate >= 70
                  ? 'bg-amber-500'
                  : 'bg-emerald-500'
              }\`}
              style={{ width: \`\${utilizationRate}%\` }}
            />
          </div>
        </div>
      )}

      {/* Date Filter Bar */}
      <div className="bg-white dark:bg-slate-800 p-4 rounded-2xl border border-slate-200/80 dark:border-slate-700 shadow-sm flex flex-wrap items-center gap-3 print:hidden">
        <div className="flex items-center gap-2 text-xs text-slate-500">
          <Calendar className="w-4 h-4 text-slate-400" />
          <span>تصفية الفترة:</span>
        </div>

        <div className="flex items-center gap-2">
          <label className="text-xs text-slate-400">من:</label>
          <input
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
            className="px-3 py-1.5 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-xs text-slate-900 dark:text-white focus:outline-none"
          />
        </div>

        <div className="flex items-center gap-2">
          <label className="text-xs text-slate-400">إلى:</label>
          <input
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
            className="px-3 py-1.5 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-xs text-slate-900 dark:text-white focus:outline-none"
          />
        </div>

        {(fromDate || toDate) && (
          <button
            type="button"
            onClick={() => {
              setFromDate('');
              setToDate('');
            }}
            className="text-xs text-primary-600 hover:text-primary-700 font-bold px-2 py-1"
          >
            إعادة تعيين
          </button>
        )}
      </div>

      {/* Ledger Table */}
      <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200/80 dark:border-slate-700 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-right text-sm">
            <thead className="bg-slate-50 dark:bg-slate-900/40 text-slate-600 dark:text-slate-400 text-xs font-bold border-b border-slate-200 dark:border-slate-700">
              <tr>
                <th className="px-5 py-3.5">التاريخ والوقت</th>
                <th className="px-4 py-3.5">نوع الحركة</th>
                <th className="px-4 py-3.5 text-center">المبلغ</th>
                <th className="px-4 py-3.5 text-center">الرصيد بعد الحركة</th>
                <th className="px-5 py-3.5 text-left">البيان والملاحظات</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-700/60">
              {isStatementLoading ? (
                <tr>
                  <td colSpan={5} className="text-center py-12 text-slate-400 text-sm">
                    جاري تحميل كشف الحساب...
                  </td>
                </tr>
              ) : transactions.length === 0 ? (
                <tr>
                  <td colSpan={5} className="text-center py-12">
                    <div className="flex flex-col items-center justify-center text-slate-400">
                      <FileText className="w-12 h-12 stroke-[1.5] text-slate-300 dark:text-slate-600 mb-2" />
                      <p className="text-sm font-semibold">لا توجد حركات مالية مسجلة في هذه الفترة</p>
                    </div>
                  </td>
                </tr>
              ) : (
                transactions.map((tx) => {
                  const isDebit = tx.amount > 0; // Purchase increases debt
                  const isCredit = tx.amount < 0; // Payment decreases debt

                  return (
                    <tr
                      key={tx.id}
                      className="hover:bg-slate-50/70 dark:hover:bg-slate-700/30 transition-colors"
                    >
                      <td className="px-5 py-3.5 text-xs text-slate-600 dark:text-slate-400 font-mono">
                        {new Date(tx.createdAt).toLocaleString('ar-EG')}
                      </td>

                      <td className="px-4 py-3.5">
                        <div className="flex items-center gap-1.5">
                          {isDebit ? (
                            <ArrowUpRight className="w-4 h-4 text-rose-500 flex-shrink-0" />
                          ) : (
                            <ArrowDownLeft className="w-4 h-4 text-emerald-500 flex-shrink-0" />
                          )}
                          <span className="font-bold text-xs text-slate-900 dark:text-white">
                            {tx.type === 'Sale'
                              ? 'فاتورة مبيعات توريد'
                              : tx.type === 'Payment'
                              ? 'سند سداد / دفعة نقدية'
                              : tx.type === 'Refund'
                              ? 'مرتجع مبيعات'
                              : tx.type}
                          </span>
                        </div>
                      </td>

                      <td className="px-4 py-3.5 text-center font-mono font-bold text-sm">
                        <span className={isDebit ? 'text-rose-600' : 'text-emerald-600'}>
                          {isDebit ? '+' : ''}
                          {tx.amount.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                        </span>
                      </td>

                      <td className="px-4 py-3.5 text-center font-mono font-bold text-slate-800 dark:text-slate-200 text-sm">
                        {tx.balanceAfter.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                      </td>

                      <td className="px-5 py-3.5 text-left text-xs text-slate-500 dark:text-slate-400">
                        {tx.notes || (tx.referenceId ? \`مرجع: \${tx.referenceId.slice(0, 8)}\` : '—')}
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
            {statement && (
              <tfoot className="bg-slate-50 dark:bg-slate-900/60 font-bold border-t border-slate-200 dark:border-slate-700">
                <tr>
                  <td colSpan={3} className="px-5 py-3 text-left text-slate-700 dark:text-slate-300 text-xs">
                    الرصيد الختامي المستحق:
                  </td>
                  <td className="px-4 py-3 text-center font-mono text-base font-black text-slate-900 dark:text-white">
                    {statement.currentBalance.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                  </td>
                  <td className="px-5 py-3" />
                </tr>
              </tfoot>
            )}
          </table>
        </div>
      </div>
    </div>
  );
};

export default PortalStatement;
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('PortalStatement.tsx created successfully.');

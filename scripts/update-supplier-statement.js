const fs = require('fs');
const filePath = 'G:/system-analysiss-saas/system-FE/src/features/suppliers/components/SupplierStatementModal.tsx';
let content = fs.readFileSync(filePath, 'utf8');

// Ensure api client and icons are imported
if (!content.includes('api from')) {
  content = `import api from '../../../api/client';\n` + content;
}
if (!content.includes('ChevronDown')) {
  content = content.replace(
    `import { FileText, Printer, X, ArrowDownLeft, ArrowUpRight`,
    `import { FileText, Printer, X, ArrowDownLeft, ArrowUpRight, ChevronDown, ChevronUp, Eye, Loader2`
  );
}

// Add state for expanded purchase details
const targetState = `  const [fromDate, setFromDate] = useState('');\n  const [toDate, setToDate] = useState('');`;
const newState = `  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [expandedPurchaseId, setExpandedPurchaseId] = useState<string | null>(null);
  const [purchaseDetails, setPurchaseDetails] = useState<Record<string, any>>({});
  const [loadingPurchaseId, setLoadingPurchaseId] = useState<string | null>(null);

  const togglePurchaseDetails = async (purchaseId: string) => {
    if (expandedPurchaseId === purchaseId) {
      setExpandedPurchaseId(null);
      return;
    }
    setExpandedPurchaseId(purchaseId);
    if (!purchaseDetails[purchaseId]) {
      setLoadingPurchaseId(purchaseId);
      try {
        const res = await api.get(\`/purchases/\${purchaseId}\`);
        if (res.data?.data) {
          setPurchaseDetails(prev => ({ ...prev, [purchaseId]: res.data.data }));
        }
      } catch (err) {
        console.error('Failed to load purchase details', err);
      } finally {
        setLoadingPurchaseId(null);
      }
    }
  };`;

content = content.replace(targetState, newState);

// Update the table rendering to support the expandable purchase details row
const targetTrReturn = `                  return (
                    <tr key={t.id} className="hover:bg-slate-100/70 dark:hover:bg-slate-700/60 transition-colors">
                      <td className="py-2.5 px-3">{dateFormatted}</td>
                      <td className="py-2.5 px-3 font-sans font-semibold">
                        <span
                          className={\`inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] \${
                            isPayment
                              ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40'
                              : 'bg-rose-50 text-rose-700 dark:bg-rose-950/40'
                          }\`}
                        >
                          {isPayment ? <ArrowDownLeft className="w-3 h-3" /> : <ArrowUpRight className="w-3 h-3" />}
                          <span>{t.type === 'Purchase' ? 'فاتورة مشتريات' : t.type === 'Payment' ? 'سند صرف' : t.type}</span>
                        </span>
                      </td>
                      <td
                        className={\`py-2.5 px-3 text-center font-bold \${
                          isPayment ? 'text-emerald-600' : 'text-rose-600'
                        }\`}
                      >
                        {isPayment ? \`-\${t.amount.toFixed(2)}\` : \`+\${t.amount.toFixed(2)}\`}
                      </td>
                      <td className="py-2.5 px-3 text-center font-black text-slate-800 dark:text-white">
                        {t.runningBalance.toFixed(2)} ج.م
                      </td>
                      <td className="py-2.5 px-3 font-sans text-slate-700 dark:text-slate-300 text-[11px] font-medium">
                        {t.notes || t.referenceId || '-'}
                      </td>
                    </tr>
                  );`;

const newTrReturn = `                  const isExpanded = t.referenceId && expandedPurchaseId === t.referenceId;
                  const purchaseData = t.referenceId ? purchaseDetails[t.referenceId] : null;

                  return (
                    <React.Fragment key={t.id}>
                      <tr className="hover:bg-slate-100/70 dark:hover:bg-slate-700/60 transition-colors border-b border-slate-100 dark:border-slate-800">
                        <td className="py-2.5 px-3">{dateFormatted}</td>
                        <td className="py-2.5 px-3 font-sans font-semibold">
                          <span
                            className={\`inline-flex items-center gap-1 px-2 py-0.5 rounded text-[11px] \${
                              isPayment
                                ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40'
                                : 'bg-rose-50 text-rose-700 dark:bg-rose-950/40'
                            }\`}
                          >
                            {isPayment ? <ArrowDownLeft className="w-3 h-3" /> : <ArrowUpRight className="w-3 h-3" />}
                            <span>{t.type === 'Purchase' ? 'فاتورة مشتريات' : t.type === 'Payment' ? 'سند صرف' : t.type}</span>
                          </span>
                        </td>
                        <td
                          className={\`py-2.5 px-3 text-center font-bold \${
                            isPayment ? 'text-emerald-600' : 'text-rose-600'
                          }\`}
                        >
                          {isPayment ? \`-\${t.amount.toFixed(2)}\` : \`+\${t.amount.toFixed(2)}\`}
                        </td>
                        <td className="py-2.5 px-3 text-center font-black text-slate-800 dark:text-white">
                          {t.runningBalance.toFixed(2)} ج.م
                        </td>
                        <td className="py-2.5 px-3 font-sans text-slate-700 dark:text-slate-300 text-[11px] font-medium">
                          <div className="flex items-center justify-between gap-2">
                            <span>{t.notes || t.referenceId || '-'}</span>
                            {t.type === 'Purchase' && t.referenceId && (
                              <button
                                type="button"
                                onClick={() => togglePurchaseDetails(t.referenceId)}
                                className="inline-flex items-center gap-1 px-2 py-1 text-[11px] font-bold text-blue-600 hover:text-blue-700 dark:text-blue-400 bg-blue-50 dark:bg-blue-950/50 hover:bg-blue-100 rounded-lg transition-colors"
                              >
                                {loadingPurchaseId === t.referenceId ? (
                                  <Loader2 className="w-3 h-3 animate-spin" />
                                ) : isExpanded ? (
                                  <ChevronUp className="w-3 h-3" />
                                ) : (
                                  <Eye className="w-3 h-3" />
                                )}
                                <span>{isExpanded ? 'إخفاء' : 'تفاصيل الفاتورة'}</span>
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>

                      {/* Detailed Purchase Breakdown Accordion */}
                      {isExpanded && (
                        <tr className="bg-blue-50/40 dark:bg-blue-950/20 border-b border-blue-100 dark:border-blue-900/40 animate-fadeIn">
                          <td colSpan={5} className="p-3">
                            <div className="bg-white dark:bg-slate-900 rounded-xl p-3 border border-blue-200 dark:border-blue-800 shadow-inner">
                              <div className="flex items-center justify-between pb-2 mb-2 border-b border-slate-100 dark:border-slate-800 text-xs font-bold text-slate-800 dark:text-slate-200">
                                <span>📦 تفاصيل بنود الفاتورة ({purchaseData?.purchaseNumber || t.notes}):</span>
                                <span className="text-[11px] text-slate-500 font-mono">
                                  الحالة: {purchaseData?.status === 2 || purchaseData?.status === 'Confirmed' ? 'مؤكدة' : 'مسودة'}
                                </span>
                              </div>

                              {loadingPurchaseId === t.referenceId ? (
                                <div className="text-center py-4 text-xs font-bold text-slate-400 flex items-center justify-center gap-2">
                                  <Loader2 className="w-4 h-4 animate-spin text-blue-500" />
                                  <span>جاري تحميل تفاصيل الأصناف...</span>
                                </div>
                              ) : purchaseData?.items?.length ? (
                                <table className="w-full text-right text-[11px]">
                                  <thead>
                                    <tr className="text-slate-500 dark:text-slate-400 border-b border-slate-100 dark:border-slate-800">
                                      <th className="py-1 px-2">الصنف</th>
                                      <th className="py-1 px-2 text-center">الكمية المستلمة</th>
                                      <th className="py-1 px-2 text-center">سعر الشراء (التكلفة)</th>
                                      <th className="py-1 px-2 text-left">الإجمالي</th>
                                    </tr>
                                  </thead>
                                  <tbody className="divide-y divide-slate-100 dark:divide-slate-800/60 font-mono">
                                    {purchaseData.items.map((item: any, idx: number) => (
                                      <tr key={idx} className="hover:bg-slate-50 dark:hover:bg-slate-800/40">
                                        <td className="py-1.5 px-2 font-sans font-medium text-slate-800 dark:text-slate-200">
                                          {item.productName || item.productId}
                                        </td>
                                        <td className="py-1.5 px-2 text-center font-bold text-slate-700 dark:text-slate-300">
                                          {item.quantity}
                                        </td>
                                        <td className="py-1.5 px-2 text-center text-slate-600 dark:text-slate-400">
                                          {item.unitCost?.toFixed(2)} ج.م
                                        </td>
                                        <td className="py-1.5 px-2 text-left font-bold text-emerald-600 dark:text-emerald-400">
                                          {item.subTotal?.toFixed(2)} ج.م
                                        </td>
                                      </tr>
                                    ))}
                                  </tbody>
                                </table>
                              ) : (
                                <div className="text-center py-3 text-xs text-slate-400">
                                  لا توجد بنود مسجلة لهذه الفاتورة
                                </div>
                              )}
                            </div>
                          </td>
                        </tr>
                      )}
                    </React.Fragment>
                  );`;

// Regex replacement
const targetRegex = /return \(\s*<tr key=\{t\.id\}[\s\S]*?<\/tr>\s*\);/;
content = content.replace(targetRegex, newTrReturn);

fs.writeFileSync(filePath, content, 'utf8');
console.log('SupplierStatementModal updated with expandable invoice breakdown!');

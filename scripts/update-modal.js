const fs = require('fs');
const filePath = 'G:/system-analysiss-saas/system-FE/src/features/b2b/components/B2BOrderDetailsModal.tsx';
let content = fs.readFileSync(filePath, 'utf8');

// 1. Fix whatsappUrl & add hasBelowCostItem
const targetWaRegex = /\/\/\s*WhatsApp click-to-chat[\s\S]*?buildOrderWhatsAppUrl\([\s\S]*?\)\s*:\s*null;/;

const newWa = `// WhatsApp click-to-chat with real status and amounts
  const whatsappUrl = order?.merchantPhone
    ? buildOrderWhatsAppUrl(
        order.merchantPhone,
        order.orderNumber,
        order.merchantTradeName || merchant?.tradeName || 'عزيزنا التاجر',
        order.status,
        calculatedReviewTotal || order.totalAmount,
        order.paymentPreference
      )
    : null;

  // Cost guard: Detect any item adjusted below its purchase cost
  const hasBelowCostItem = isPending && (order?.items || []).some((item) => {
    const adj = adjustments[item.id];
    const price = adj ? adj.unitWholesalePrice : item.unitWholesalePrice;
    const cost = item.purchaseCost ?? 0;
    return cost > 0 && price < cost;
  });`;

content = content.replace(targetWaRegex, newWa);

// 2. Guard handleApprove
const targetApproveRegex = /const handleApprove = async \(\) => \{\s*if \(!order\) return;\s*try \{/;
const newApprove = `const handleApprove = async () => {
    if (!order) return;
    if (hasBelowCostItem) {
      toast.error('لا يمكن اعتماد الطلب: يوجد صنف تم تحديد سعر بيع له أقل من سعر التكلفة!');
      return;
    }
    try {`;
content = content.replace(targetApproveRegex, newApprove);

// 3. Update Wholesale Price input to show warning when below cost
const targetPriceInputRegex = /<input\s*type="number"\s*min="0"\s*step="0\.5"\s*value=\{adj\.unitWholesalePrice\}\s*onChange=\{\(e\) => handlePriceChange\(item\.id, parseFloat\(e\.target\.value\) \|\| 0\)\}[^>]*\/>/;

const newPriceInput = `<div>
                                    <input
                                      type="number"
                                      min={item.purchaseCost ?? 0}
                                      step="0.5"
                                      value={adj.unitWholesalePrice}
                                      onChange={(e) => handlePriceChange(item.id, parseFloat(e.target.value) || 0)}
                                      className={\`w-20 px-2 py-1 text-center font-mono font-bold rounded-lg border \${
                                        (item.purchaseCost ?? 0) > 0 && adj.unitWholesalePrice < (item.purchaseCost ?? 0)
                                          ? 'border-rose-500 text-rose-600 bg-rose-50 dark:bg-rose-950/30 ring-2 ring-rose-500/20'
                                          : 'border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-700 text-slate-900 dark:text-white'
                                      }\`}
                                    />
                                    {(item.purchaseCost ?? 0) > 0 && adj.unitWholesalePrice < (item.purchaseCost ?? 0) && (
                                      <span className="block text-[9px] text-rose-500 font-bold mt-0.5">أقل من التكلفة!</span>
                                    )}
                                  </div>`;

content = content.replace(targetPriceInputRegex, newPriceInput);

// 4. Disable approve button if hasBelowCostItem
content = content.replace(
  'disabled={approveMutation.isPending}',
  'disabled={approveMutation.isPending || hasBelowCostItem}'
);

fs.writeFileSync(filePath, content, 'utf8');
console.log('B2BOrderDetailsModal updated successfully!');

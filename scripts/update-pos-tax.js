const fs = require('fs');

// 1. Update usePosCart.ts
const posCartPath = 'G:/system-analysiss-saas/system-FE/src/features/pos/hooks/usePosCart.ts';
let posCartContent = fs.readFileSync(posCartPath, 'utf8');

if (!posCartContent.includes('settingsApi')) {
  posCartContent = `import { settingsApi } from '../../settings/api/settingsApi';\n` + posCartContent;
}

const targetTotalsRegex = /\/\/ Memoized Totals Calculation[\s\S]*?return \{\s*subtotal,[\s\S]*?totalUnits,\s*\};\s*\}, \[items, overallDiscount\]\);/;

const newTotalsLogic = `  // Check if VAT 14% is enabled for store
  const [isTaxEnabled, setIsTaxEnabled] = useState<boolean>(() => {
    try {
      return localStorage.getItem('retailos_store_tax_enabled') === 'true';
    } catch {
      return false;
    }
  });

  useEffect(() => {
    settingsApi.getStoreProfile().then((store) => {
      if (store) {
        setIsTaxEnabled(!!store.taxEnabled);
        localStorage.setItem('retailos_store_tax_enabled', String(!!store.taxEnabled));
      }
    }).catch(() => {});
  }, []);

  // Memoized Totals Calculation
  const totals: CartTotals = useMemo(() => {
    let subtotal = 0;
    let lineDiscounts = 0;
    let totalUnits = 0;

    for (let i = 0; i < items.length; i++) {
      const item = items[i];
      subtotal += item.unitPrice * item.quantity;
      lineDiscounts += item.discount;
      totalUnits += item.quantity;
    }

    const totalDiscount = lineDiscounts + (overallDiscount || 0);
    const taxableSubtotal = Math.max(0, subtotal - totalDiscount);
    const taxAmount = isTaxEnabled ? Number((taxableSubtotal * 0.14).toFixed(2)) : 0;
    const grandTotal = taxableSubtotal + taxAmount;

    return {
      subtotal,
      totalDiscount,
      taxAmount,
      grandTotal,
      itemCount: items.length,
      totalUnits,
    };
  }, [items, overallDiscount, isTaxEnabled]);`;

posCartContent = posCartContent.replace(targetTotalsRegex, newTotalsLogic);
fs.writeFileSync(posCartPath, posCartContent, 'utf8');
console.log('usePosCart.ts updated with dynamic 14% VAT calculation!');

// 2. Update PosSummary.tsx to display tax row when taxAmount > 0
const posSummaryPath = 'G:/system-analysiss-saas/system-FE/src/features/pos/components/PosSummary.tsx';
let posSummaryContent = fs.readFileSync(posSummaryPath, 'utf8');

const targetTotalDiscount = /\{totals\.totalDiscount > 0 && \([\s\S]*?\)\}/;
const match = posSummaryContent.match(targetTotalDiscount);
if (match) {
  const taxRow = `\n\n        {totals.taxAmount > 0 && (
          <div className="flex justify-between text-emerald-600 dark:text-emerald-400 font-bold text-xs">
            <span>ضريبة القيمة المضافة (14% VAT):</span>
            <span className="font-mono">+{totals.taxAmount.toFixed(2)} ج.م</span>
          </div>
        )}`;
  posSummaryContent = posSummaryContent.replace(match[0], match[0] + taxRow);
  fs.writeFileSync(posSummaryPath, posSummaryContent, 'utf8');
  console.log('PosSummary.tsx updated with VAT display row!');
}

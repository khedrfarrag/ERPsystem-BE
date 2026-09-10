const fs = require('fs');
const path = require('path');

// 1. Update products.schemas.ts
const schemasPath = path.resolve(__dirname, '../../system-FE/src/features/products/types/products.schemas.ts');
let schemasContent = fs.readFileSync(schemasPath, 'utf8');

if (!schemasContent.includes('isWholesaleAvailable')) {
  schemasContent = schemasContent.replace(
    "  minStockLevel: z.number().min(0, 'حد الطلب الأدنى يجب أن يكون 0 أو أكثر'),",
    "  minStockLevel: z.number().min(0, 'حد الطلب الأدنى يجب أن يكون 0 أو أكثر'),\n  wholesalePrice: z.number().min(0, 'سعر الجملة لا يمكن أن يكون سالباً').optional().nullable(),\n  isWholesaleAvailable: z.boolean().default(false),"
  );
  fs.writeFileSync(schemasPath, schemasContent, 'utf8');
  console.log('products.schemas.ts updated.');
}

// 2. Update products.types.ts
const typesPath = path.resolve(__dirname, '../../system-FE/src/features/products/types/products.types.ts');
let typesContent = fs.readFileSync(typesPath, 'utf8');

if (!typesContent.includes('wholesalePrice?: number')) {
  typesContent = typesContent.replace(
    "  sellingPrice: number;\n  purchaseCost?: number;",
    "  sellingPrice: number;\n  wholesalePrice?: number;\n  isWholesaleAvailable?: boolean;\n  purchaseCost?: number;"
  );
  typesContent = typesContent.replace(
    "  sellingPrice: number;\n  minStockLevel: number;\n  initialStock?: number;",
    "  sellingPrice: number;\n  wholesalePrice?: number;\n  isWholesaleAvailable?: boolean;\n  minStockLevel: number;\n  initialStock?: number;"
  );
  typesContent = typesContent.replace(
    "  sellingPrice: number;\n  minStockLevel: number;\n}",
    "  sellingPrice: number;\n  wholesalePrice?: number;\n  isWholesaleAvailable?: boolean;\n  minStockLevel: number;\n}"
  );
  fs.writeFileSync(typesPath, typesContent, 'utf8');
  console.log('products.types.ts updated.');
}

// 3. Update ProductModal.tsx
const modalPath = path.resolve(__dirname, '../../system-FE/src/features/products/components/ProductModal.tsx');
let modalContent = fs.readFileSync(modalPath, 'utf8');

if (!modalContent.includes('Store,')) {
  modalContent = modalContent.replace(
    "import { Package, X, Loader2, Plus, AlertTriangle, CheckCircle2, DollarSign } from 'lucide-react';",
    "import { Package, X, Loader2, Plus, AlertTriangle, CheckCircle2, DollarSign, Store } from 'lucide-react';"
  );
}

if (!modalContent.includes('isWholesaleAvailable: false')) {
  modalContent = modalContent.replace(
    "      initialStock: 0,\n    },",
    "      initialStock: 0,\n      wholesalePrice: null,\n      isWholesaleAvailable: false,\n    },"
  );
}

if (!modalContent.includes('watchWholesalePrice')) {
  modalContent = modalContent.replace(
    "  const watchPrice = watch('sellingPrice') || 0;",
    "  const watchPrice = watch('sellingPrice') || 0;\n  const watchWholesalePrice = watch('wholesalePrice');\n  const watchWholesaleAvailable = watch('isWholesaleAvailable');"
  );

  const wholesaleCalc = `  // Wholesale Profit Calculation
  const wholesaleMargin = useMemo(() => {
    const effectivePrice = (watchWholesalePrice && watchWholesalePrice > 0) ? watchWholesalePrice : watchPrice;
    const profit = effectivePrice - watchCost;
    const margin = effectivePrice > 0 ? (profit / effectivePrice) * 100 : 0;
    return {
      effectivePrice,
      profit,
      margin,
      isLoss: profit < 0,
      isFallback: !watchWholesalePrice || watchWholesalePrice <= 0,
    };
  }, [watchCost, watchPrice, watchWholesalePrice]);
`;

  modalContent = modalContent.replace(
    "  useEffect(() => {",
    wholesaleCalc + "\n  useEffect(() => {"
  );
}

if (!modalContent.includes('wholesalePrice: productToEdit.wholesalePrice')) {
  modalContent = modalContent.replace(
    "        minStockLevel: productToEdit.minStockLevel || 0,\n        initialStock: 0,",
    "        minStockLevel: productToEdit.minStockLevel || 0,\n        initialStock: 0,\n        wholesalePrice: productToEdit.wholesalePrice ?? null,\n        isWholesaleAvailable: productToEdit.isWholesaleAvailable ?? false,"
  );

  modalContent = modalContent.replace(
    "        minStockLevel: 5,\n        initialStock: 0,\n      });",
    "        minStockLevel: 5,\n        initialStock: 0,\n        wholesalePrice: null,\n        isWholesaleAvailable: false,\n      });"
  );
}

if (!modalContent.includes('بوابة بيع الجملة (B2B)')) {
  const b2bSection = `
            {/* B2B Wholesale Pricing Section */}
            <div className="p-4 rounded-2xl bg-indigo-50/50 dark:bg-indigo-950/20 border border-indigo-200/80 dark:border-indigo-800/60 space-y-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="w-8 h-8 rounded-lg bg-indigo-500/10 text-indigo-500 flex items-center justify-center font-bold">
                    <Store className="w-4 h-4" />
                  </div>
                  <div>
                    <span className="text-xs font-bold text-slate-800 dark:text-slate-200">
                      بوابة بيع الجملة (B2B)
                    </span>
                    <p className="text-[11px] text-slate-500 dark:text-slate-400">
                      إتاحة هذا المنتج لطلبات التوريد والمتاجر الشريكة
                    </p>
                  </div>
                </div>

                <label className="relative inline-flex items-center cursor-pointer">
                  <input
                    type="checkbox"
                    {...register('isWholesaleAvailable')}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-slate-300 dark:bg-slate-700 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
                </label>
              </div>

              {watchWholesaleAvailable && (
                <div className="pt-2 border-t border-indigo-200/40 dark:border-indigo-800/40 space-y-3 animate-fadeIn">
                  <div>
                    <label className="block text-xs font-bold text-slate-700 dark:text-slate-300 mb-1">
                      سعر الجملة المخصص (ج.م) <span className="text-[11px] font-normal text-slate-400">(اختياري - يترك فارغاً لاستخدام سعر البيع)</span>
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      {...register('wholesalePrice', { 
                        setValueAs: v => (v === '' || v === null || isNaN(v) ? null : Number(v)) 
                      })}
                      placeholder={\`سعر البيع العادي (\${watchPrice || 0} ج.م)\`}
                      className="w-full px-3 py-2 bg-white dark:bg-slate-800 border border-indigo-200 dark:border-indigo-700/60 rounded-xl text-sm font-bold font-mono text-slate-900 dark:text-white focus:outline-none focus:border-indigo-500"
                    />
                    {errors.wholesalePrice && (
                      <p className="text-[10px] text-rose-500 mt-1">{errors.wholesalePrice.message}</p>
                    )}
                  </div>

                  {/* Wholesale Profit Indicator */}
                  <div
                    className={\`p-2.5 rounded-xl flex items-center justify-between border text-xs font-semibold \${
                      wholesaleMargin.isLoss
                        ? 'bg-rose-50 dark:bg-rose-950/40 border-rose-200 dark:border-rose-800 text-rose-800 dark:text-rose-300'
                        : 'bg-indigo-50 dark:bg-indigo-950/40 border-indigo-200 dark:border-indigo-800 text-indigo-800 dark:text-indigo-300'
                    }\`}
                  >
                    <div className="flex items-center gap-1.5">
                      {wholesaleMargin.isLoss ? (
                        <AlertTriangle className="w-4 h-4 text-rose-500" />
                      ) : (
                        <CheckCircle2 className="w-4 h-4 text-indigo-500" />
                      )}
                      <span>
                        سعر الجملة الفعلي: <span className="font-mono font-bold">{wholesaleMargin.effectivePrice}</span> ج.م
                        {wholesaleMargin.isFallback && ' (تطبيق سعر المستهلك)'}
                      </span>
                    </div>

                    <div className="flex items-center gap-2">
                      <span>هامش الجملة:</span>
                      <span className="font-mono font-bold">{wholesaleMargin.margin.toFixed(1)}%</span>
                      <span>(ربح: {wholesaleMargin.profit.toFixed(2)} ج.م)</span>
                    </div>
                  </div>
                </div>
              )}
            </div>
`;

  modalContent = modalContent.replace(
    "            {/* Initial Stock (Only on Creation) */}",
    b2bSection + "\n            {/* Initial Stock (Only on Creation) */}"
  );
}

fs.writeFileSync(modalPath, modalContent, 'utf8');
console.log('ProductModal.tsx updated successfully.');

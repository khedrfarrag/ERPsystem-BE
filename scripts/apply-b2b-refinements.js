const fs = require('fs');
const path = require('path');

const feRoot = path.resolve(__dirname, '../../system-FE');

// 1. Update b2b.types.ts
const typesPath = path.join(feRoot, 'src/features/b2b/types/b2b.types.ts');
let typesContent = fs.readFileSync(typesPath, 'utf8');

if (!typesContent.includes('expectedDownPayment?: number')) {
  typesContent = typesContent.replace(
    'paymentPreference: B2BPaymentPreference;\n  totalAmount: number;',
    'paymentPreference: B2BPaymentPreference;\n  expectedDownPayment?: number | null;\n  discountAmount?: number;\n  totalAmount: number;'
  );
}

if (!typesContent.includes('availableStock?: number;')) {
  typesContent = typesContent.replace(
    'adjustmentReason?: string | null;\n  adjustedAt?: string | null;',
    'adjustmentReason?: string | null;\n  adjustedAt?: string | null;\n  availableStock?: number;\n  purchaseCost?: number | null;'
  );
}

if (!typesContent.includes('expectedDownPayment?: number;')) {
  typesContent = typesContent.replace(
    'paymentPreference: B2BPaymentPreference;\n  notes?: string;',
    'paymentPreference: B2BPaymentPreference;\n  expectedDownPayment?: number;\n  notes?: string;'
  );
}

if (!typesContent.includes('unitWholesalePrice?: number;')) {
  typesContent = typesContent.replace(
    'approvedQuantity: number;\n    adjustmentReason?: string;',
    'approvedQuantity: number;\n    unitWholesalePrice?: number;\n    adjustmentReason?: string;'
  );
}

if (!typesContent.includes('discountAmount?: number;')) {
  typesContent = typesContent.replace(
    'paidAmount: number;\n  paymentMethod: string;',
    'paidAmount: number;\n  discountAmount?: number;\n  paymentMethod: string;'
  );
}

fs.writeFileSync(typesPath, typesContent, 'utf8');
console.log('✓ b2b.types.ts updated');

// 2. Update PortalCatalog.tsx
const catalogPath = path.join(feRoot, 'src/pages/portal/PortalCatalog.tsx');
let catalogContent = fs.readFileSync(catalogPath, 'utf8');

// Ensure handleAddToCart guards against zero stock
catalogContent = catalogContent.replace(
  `  const handleAddToCart = (product: B2BCatalogProduct) => {\n    setCart((prev) => {`,
  `  const handleAddToCart = (product: B2BCatalogProduct) => {\n    if (product.availableStock <= 0) return;\n    setCart((prev) => {`
);

// Ensure out of stock shows disabled button
const oldAddToCartBtn = `{inCart ? (
                    <div className="flex items-center gap-1 bg-emerald-600/20 border border-emerald-500 text-emerald-300 px-3 py-1.5 rounded-xl text-xs font-bold">
                      <Check className="w-3.5 h-3.5" />
                      <span>{inCart.quantity} في السلة</span>
                    </div>
                  ) : (
                    <button
                      type="button"
                      onClick={() => handleAddToCart(product)}
                      className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-slate-800 hover:bg-emerald-600 text-white font-semibold text-xs transition hover:shadow-md hover:shadow-emerald-600/20"
                    >
                      <Plus className="w-3.5 h-3.5" />
                      <span>إضافة</span>
                    </button>
                  )}`;

const newAddToCartBtn = `{inCart ? (
                    <div className="flex items-center gap-1 bg-emerald-600/20 border border-emerald-500 text-emerald-300 px-3 py-1.5 rounded-xl text-xs font-bold">
                      <Check className="w-3.5 h-3.5" />
                      <span>{inCart.quantity} في السلة</span>
                    </div>
                  ) : isOutOfStock ? (
                    <button
                      type="button"
                      disabled
                      className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-slate-800/40 text-slate-500 font-semibold text-xs cursor-not-allowed border border-slate-800"
                    >
                      <span>نفد المخزون</span>
                    </button>
                  ) : (
                    <button
                      type="button"
                      onClick={() => handleAddToCart(product)}
                      className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-slate-800 hover:bg-emerald-600 text-white font-semibold text-xs transition hover:shadow-md hover:shadow-emerald-600/20"
                    >
                      <Plus className="w-3.5 h-3.5" />
                      <span>إضافة</span>
                    </button>
                  )}`;

if (catalogContent.includes(oldAddToCartBtn)) {
  catalogContent = catalogContent.replace(oldAddToCartBtn, newAddToCartBtn);
}

fs.writeFileSync(catalogPath, catalogContent, 'utf8');
console.log('✓ PortalCatalog.tsx updated');

// 3. Update PortalOrders.tsx to show rejection reason prominently
const portalOrdersPath = path.join(feRoot, 'src/pages/portal/PortalOrders.tsx');
let ordersContent = fs.readFileSync(portalOrdersPath, 'utf8');

const statusBadgeTarget = `{order.status === 'Rejected' && <XCircle className="w-3 h-3" />}
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
                      </div>`;

const statusBadgeReplacement = `{order.status === 'Rejected' && <XCircle className="w-3 h-3" />}
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

                      {order.status === 'Rejected' && order.rejectionReason && (
                        <div className="mt-2 flex items-start gap-2 p-2 bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 rounded-xl text-xs text-rose-700 dark:text-rose-300">
                          <XCircle className="w-4 h-4 flex-shrink-0 mt-0.5 text-rose-500" />
                          <div>
                            <span className="font-bold">سبب الرفض من الإدارة: </span>
                            <span>{order.rejectionReason}</span>
                          </div>
                        </div>
                      )}`;

if (ordersContent.includes(statusBadgeTarget)) {
  ordersContent = ordersContent.replace(statusBadgeTarget, statusBadgeReplacement);
  fs.writeFileSync(portalOrdersPath, ordersContent, 'utf8');
  console.log('✓ PortalOrders.tsx updated');
} else {
  console.log('! PortalOrders.tsx badge pattern not found directly, checking...');
}

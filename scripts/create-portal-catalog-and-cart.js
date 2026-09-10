const fs = require('fs');
const path = require('path');

const cartModalPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/components/PortalCartModal.tsx');
const catalogPagePath = path.resolve(__dirname, '../../system-FE/src/pages/portal/PortalCatalog.tsx');

const cartModalContent = `import React, { useState } from 'react';
import { B2BCatalogProduct, B2BPaymentPreference } from '../types/b2b.types';
import { useCreateB2BOrder } from '../hooks/useB2BOrders';
import { 
  X, 
  ShoppingCart, 
  Trash2, 
  Plus, 
  Minus, 
  CreditCard, 
  Banknote, 
  Send,
  FileText,
  AlertCircle
} from 'lucide-react';

export interface CartItem {
  product: B2BCatalogProduct;
  quantity: number;
}

interface PortalCartModalProps {
  isOpen: boolean;
  onClose: () => void;
  cart: CartItem[];
  onUpdateQuantity: (productId: string, quantity: number) => void;
  onRemoveItem: (productId: string) => void;
  onClearCart: () => void;
  onOrderSuccess: (orderNumber: string) => void;
}

export const PortalCartModal: React.FC<PortalCartModalProps> = ({
  isOpen,
  onClose,
  cart,
  onUpdateQuantity,
  onRemoveItem,
  onClearCart,
  onOrderSuccess,
}) => {
  const [paymentPreference, setPaymentPreference] = useState<B2BPaymentPreference>('Credit');
  const [notes, setNotes] = useState('');

  const createOrderMutation = useCreateB2BOrder();

  if (!isOpen) return null;

  const totalAmount = cart.reduce(
    (acc, item) => acc + item.quantity * item.product.effectiveWholesalePrice,
    0
  );

  const totalUnits = cart.reduce((acc, item) => acc + item.quantity, 0);

  const handleSubmitOrder = async () => {
    if (cart.length === 0) return;

    try {
      const payload = {
        paymentPreference,
        notes: notes.trim() || undefined,
        items: cart.map((c) => ({
          productId: c.product.id,
          quantity: c.quantity,
        })),
      };

      const res = await createOrderMutation.mutateAsync(payload);
      onClearCart();
      onClose();
      onOrderSuccess(res.orderNumber);
    } catch (err) {
      // Toast error handled by mutation
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm animate-fadeIn" dir="rtl">
      <div className="bg-slate-900 border border-slate-800 rounded-3xl max-w-2xl w-full p-6 shadow-2xl relative max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between pb-4 border-b border-slate-800 flex-shrink-0">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-emerald-600/20 text-emerald-400 flex items-center justify-center font-bold">
              <ShoppingCart className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-white">سلة طلب التوريد</h2>
              <p className="text-xs text-slate-400">
                مراجعة الكميات وتحديد شروط الدفع قبل إرسال الطلب
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-xl text-slate-400 hover:text-white hover:bg-slate-800 transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Cart Items List */}
        <div className="flex-1 overflow-y-auto py-4 space-y-3">
          {cart.length === 0 ? (
            <div className="text-center py-12">
              <ShoppingCart className="w-12 h-12 text-slate-600 mx-auto mb-3" />
              <p className="text-sm font-semibold text-slate-400">سلة الطلبات فارغة</p>
              <p className="text-xs text-slate-500 mt-1">تصفح الكتالوج وأضف المنتجات المطلوبة للبدء.</p>
            </div>
          ) : (
            cart.map(({ product, quantity }) => {
              const itemTotal = quantity * product.effectiveWholesalePrice;
              const isOverStock = product.availableStock > 0 && quantity > product.availableStock;

              return (
                <div
                  key={product.id}
                  className="bg-slate-950 border border-slate-800/80 rounded-2xl p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 hover:border-slate-700 transition"
                >
                  <div className="flex-1">
                    <div className="font-bold text-white text-sm">{product.name}</div>
                    <div className="text-xs text-slate-400 mt-0.5 flex items-center gap-2">
                      <span>سعر الجملة: <strong className="text-emerald-400 font-mono">{product.effectiveWholesalePrice} ج.م</strong></span>
                      {product.unitName && <span>({product.unitName})</span>}
                    </div>
                    {isOverStock && (
                      <div className="flex items-center gap-1 text-amber-400 text-[11px] mt-1">
                        <AlertCircle className="w-3.5 h-3.5 flex-shrink-0" />
                        <span>الكمية المطلوبة تتجاوز المخزون الحالي ({product.availableStock}). ستتم مراجعتها من قِبل الإدارة.</span>
                      </div>
                    )}
                  </div>

                  {/* Stepper & Total */}
                  <div className="flex items-center justify-between sm:justify-end gap-4">
                    <div className="flex items-center bg-slate-900 border border-slate-700 rounded-xl p-1">
                      <button
                        type="button"
                        onClick={() => onUpdateQuantity(product.id, quantity - 1)}
                        className="w-7 h-7 rounded-lg bg-slate-800 hover:bg-slate-700 text-white flex items-center justify-center transition"
                      >
                        <Minus className="w-3.5 h-3.5" />
                      </button>
                      <input
                        type="number"
                        min="1"
                        value={quantity}
                        onChange={(e) => onUpdateQuantity(product.id, Math.max(1, parseInt(e.target.value) || 1))}
                        className="w-12 text-center bg-transparent text-sm font-mono font-bold text-white focus:outline-none"
                      />
                      <button
                        type="button"
                        onClick={() => onUpdateQuantity(product.id, quantity + 1)}
                        className="w-7 h-7 rounded-lg bg-slate-800 hover:bg-slate-700 text-white flex items-center justify-center transition"
                      >
                        <Plus className="w-3.5 h-3.5" />
                      </button>
                    </div>

                    <div className="text-left w-24">
                      <div className="font-mono font-bold text-white text-sm">
                        {itemTotal.toLocaleString('ar-EG', { minimumFractionDigits: 2 })}
                      </div>
                      <span className="text-[10px] text-slate-500">ج.م</span>
                    </div>

                    <button
                      onClick={() => onRemoveItem(product.id)}
                      className="p-2 rounded-lg text-slate-500 hover:text-rose-400 hover:bg-rose-500/10 transition"
                      title="حذف من السلة"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              );
            })
          )}
        </div>

        {cart.length > 0 && (
          <div className="pt-4 border-t border-slate-800 flex-shrink-0 space-y-4">
            {/* Preferences & Notes */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {/* Payment Preference */}
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">
                  طريقة الدفع المفضلة
                </label>
                <div className="grid grid-cols-3 gap-2">
                  <button
                    type="button"
                    onClick={() => setPaymentPreference('Credit')}
                    className={\`py-2 px-2.5 rounded-xl border text-xs font-bold transition flex flex-col items-center gap-1 \${
                      paymentPreference === 'Credit'
                        ? 'bg-emerald-600/20 border-emerald-500 text-emerald-300'
                        : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-white'
                    }\`}
                  >
                    <CreditCard className="w-4 h-4" />
                    <span>على الحساب (آجل)</span>
                  </button>

                  <button
                    type="button"
                    onClick={() => setPaymentPreference('Cash')}
                    className={\`py-2 px-2.5 rounded-xl border text-xs font-bold transition flex flex-col items-center gap-1 \${
                      paymentPreference === 'Cash'
                        ? 'bg-emerald-600/20 border-emerald-500 text-emerald-300'
                        : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-white'
                    }\`}
                  >
                    <Banknote className="w-4 h-4" />
                    <span>كاش عند الاستلام</span>
                  </button>

                  <button
                    type="button"
                    onClick={() => setPaymentPreference('Partial')}
                    className={\`py-2 px-2.5 rounded-xl border text-xs font-bold transition flex flex-col items-center gap-1 \${
                      paymentPreference === 'Partial'
                        ? 'bg-emerald-600/20 border-emerald-500 text-emerald-300'
                        : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-white'
                    }\`}
                  >
                    <FileText className="w-4 h-4" />
                    <span>دفعة مقدمة + آجل</span>
                  </button>
                </div>
              </div>

              {/* Delivery Notes */}
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">
                  ملاحظات وتعليمات التوصيل (اختياري)
                </label>
                <textarea
                  rows={2}
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  placeholder="مثال: يرجى التوصيل قبل الساعة 2 ظهراً..."
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl p-2.5 text-xs text-white placeholder:text-slate-500 focus:outline-none focus:border-emerald-500 transition resize-none"
                />
              </div>
            </div>

            {/* Total Bar & Submit Button */}
            <div className="flex items-center justify-between bg-slate-950 p-4 rounded-2xl border border-slate-800">
              <div>
                <div className="text-xs text-slate-400">
                  إجمالي الأصناف: <span className="font-bold text-white">{totalUnits} قطعة</span>
                </div>
                <div className="text-xl font-extrabold text-emerald-400 font-mono mt-0.5">
                  {totalAmount.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} <span className="text-xs font-normal">ج.م</span>
                </div>
              </div>

              <button
                type="button"
                disabled={createOrderMutation.isPending}
                onClick={handleSubmitOrder}
                className="flex items-center gap-2 px-6 py-3 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white font-bold text-sm shadow-lg shadow-emerald-600/30 transition-all hover:scale-[1.02] active:scale-[0.98] disabled:opacity-50"
              >
                <Send className="w-4 h-4" />
                <span>{createOrderMutation.isPending ? 'جاري الإرسال...' : 'تأكيد وإرسال الطلب'}</span>
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
`;

const catalogPageContent = `import React, { useState } from 'react';
import { useB2BCatalog } from '../../features/b2b/hooks/useB2BOrders';
import { useCurrentMerchant } from '../../features/b2b/hooks/useMerchants';
import { PortalCartModal, CartItem } from '../../features/b2b/components/PortalCartModal';
import { B2BCatalogProduct } from '../../features/b2b/types/b2b.types';
import { 
  ShoppingBag, 
  Search, 
  ShoppingCart, 
  Plus, 
  Check, 
  AlertCircle, 
  CreditCard, 
  Coins, 
  Store
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';

export const PortalCatalog: React.FC = () => {
  const [search, setSearch] = useState('');
  const [cart, setCart] = useState<CartItem[]>([]);
  const [isCartOpen, setIsCartOpen] = useState(false);
  const navigate = useNavigate();

  const { data: catalog = [], isLoading } = useB2BCatalog({
    search: search.trim() || undefined,
  });

  const { data: merchantProfile } = useCurrentMerchant();

  // Cart operations
  const handleAddToCart = (product: B2BCatalogProduct) => {
    setCart((prev) => {
      const existing = prev.find((item) => item.product.id === product.id);
      if (existing) {
        return prev.map((item) =>
          item.product.id === product.id ? { ...item, quantity: item.quantity + 1 } : item
        );
      }
      return [...prev, { product, quantity: 1 }];
    });
  };

  const handleUpdateQuantity = (productId: string, quantity: number) => {
    if (quantity <= 0) {
      handleRemoveItem(productId);
      return;
    }
    setCart((prev) =>
      prev.map((item) => (item.product.id === productId ? { ...item, quantity } : item))
    );
  };

  const handleRemoveItem = (productId: string) => {
    setCart((prev) => prev.filter((item) => item.product.id !== productId));
  };

  const handleClearCart = () => {
    setCart([]);
  };

  const totalCartUnits = cart.reduce((acc, item) => acc + item.quantity, 0);
  const totalCartAmount = cart.reduce(
    (acc, item) => acc + item.quantity * item.product.effectiveWholesalePrice,
    0
  );

  return (
    <div className="space-y-6 animate-fadeIn pb-24" dir="rtl">
      {/* Merchant Financial Greeting Bar */}
      {merchantProfile && (
        <div className="bg-slate-900 border border-slate-800 p-5 rounded-3xl flex flex-col sm:flex-row items-center justify-between gap-4 shadow-sm">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-emerald-500/10 text-emerald-400 flex items-center justify-center font-bold">
              <Store className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-base font-bold text-white">{merchantProfile.tradeName}</h2>
              <p className="text-xs text-slate-400">المسئول: {merchantProfile.contactPerson}</p>
            </div>
          </div>

          <div className="flex items-center gap-6 text-xs">
            <div className="flex items-center gap-2 bg-slate-950 px-4 py-2 rounded-2xl border border-slate-800">
              <CreditCard className="w-4 h-4 text-emerald-400" />
              <div>
                <span className="text-slate-400 block text-[10px]">سقف الائتمان</span>
                <span className="font-mono font-bold text-white">
                  {merchantProfile.creditLimit.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                </span>
              </div>
            </div>

            <div className="flex items-center gap-2 bg-slate-950 px-4 py-2 rounded-2xl border border-slate-800">
              <Coins className="w-4 h-4 text-amber-400" />
              <div>
                <span className="text-slate-400 block text-[10px]">الرصيد القائم الحالي</span>
                <span className="font-mono font-bold text-amber-400">
                  {merchantProfile.currentBalance.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                </span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Header & Search */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-slate-900 border border-slate-800 p-6 rounded-3xl shadow-sm">
        <div className="flex items-center gap-4">
          <div className="w-12 h-12 rounded-2xl bg-gradient-to-tr from-emerald-600 to-teal-500 flex items-center justify-center text-white shadow-lg shadow-emerald-500/20">
            <ShoppingBag className="w-6 h-6" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-white tracking-tight">كتالوج منتجات الجملة</h1>
            <p className="text-sm text-slate-400 mt-0.5">
              تصفح أسعار الجملة المتاحة وأضف الكميات المطلوبة لسلة التوريد.
            </p>
          </div>
        </div>

        <div className="relative w-full sm:w-80">
          <Search className="w-4 h-4 absolute right-3.5 top-3.5 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="بحث في المنتجات أو الباركود..."
            className="w-full bg-slate-950 border border-slate-800 rounded-xl py-2.5 pr-10 pl-4 text-sm text-white placeholder:text-slate-500 focus:outline-none focus:border-emerald-500 transition"
          />
        </div>
      </div>

      {/* Product Catalog Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4 animate-pulse">
          {[1, 2, 3, 4, 5, 6, 7, 8].map((n) => (
            <div key={n} className="bg-slate-900 border border-slate-800 rounded-2xl h-48"></div>
          ))}
        </div>
      ) : catalog.length === 0 ? (
        <div className="bg-slate-900 border border-slate-800 rounded-3xl p-12 text-center">
          <ShoppingBag className="w-12 h-12 text-slate-600 mx-auto mb-3" />
          <h3 className="text-lg font-bold text-white mb-1">لا توجد منتجات جملة مطابقة</h3>
          <p className="text-sm text-slate-400">جرب البحث بكلمة أخرى أو تواصل مع إدارة المتجر.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {catalog.map((product) => {
            const inCart = cart.find((i) => i.product.id === product.id);
            const isOutOfStock = product.availableStock <= 0;

            return (
              <div
                key={product.id}
                className="bg-slate-900 border border-slate-800 rounded-2xl p-5 flex flex-col justify-between hover:border-slate-700 transition shadow-sm"
              >
                <div>
                  <div className="flex items-start justify-between gap-2">
                    <span className="text-[11px] font-semibold text-slate-400 bg-slate-950 px-2 py-0.5 rounded-md border border-slate-800">
                      {product.categoryName || 'عام'}
                    </span>
                    <span
                      className={\`text-[11px] font-bold px-2 py-0.5 rounded-md flex items-center gap-1 \${
                        isOutOfStock
                          ? 'bg-rose-500/10 text-rose-400 border border-rose-500/20'
                          : product.availableStock <= 5
                          ? 'bg-amber-500/10 text-amber-400 border border-amber-500/20'
                          : 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20'
                      }\`}
                    >
                      {isOutOfStock ? 'نفد المخزون' : \`متاح: \${product.availableStock}\`}
                    </span>
                  </div>

                  <h3 className="font-bold text-white text-base mt-3 leading-snug line-clamp-2">
                    {product.name}
                  </h3>

                  {product.barcode && (
                    <p className="text-xs font-mono text-slate-500 mt-1" dir="ltr">
                      {product.barcode}
                    </p>
                  )}
                </div>

                <div className="pt-4 border-t border-slate-800/80 mt-4 flex items-center justify-between">
                  <div>
                    <span className="text-[10px] text-slate-400 block">سعر الجملة</span>
                    <div className="font-mono font-extrabold text-emerald-400 text-lg">
                      {product.effectiveWholesalePrice.toLocaleString('ar-EG', {
                        minimumFractionDigits: 2,
                      })}{' '}
                      <span className="text-xs font-normal">ج.م</span>
                    </div>
                  </div>

                  {inCart ? (
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
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Floating Bottom Cart Bar */}
      {cart.length > 0 && (
        <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-40 max-w-lg w-full px-4 animate-slideUp">
          <div className="bg-slate-900/95 backdrop-blur-md border border-slate-700 shadow-2xl rounded-2xl p-3 flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-emerald-600 text-white flex items-center justify-center font-bold relative">
                <ShoppingCart className="w-5 h-5" />
                <span className="absolute -top-1.5 -right-1.5 w-5 h-5 bg-rose-500 text-white text-[10px] font-bold rounded-full flex items-center justify-center border-2 border-slate-900">
                  {totalCartUnits}
                </span>
              </div>
              <div>
                <span className="text-xs text-slate-400 block">إجمالي السلة</span>
                <span className="font-mono font-extrabold text-white text-base">
                  {totalCartAmount.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                </span>
              </div>
            </div>

            <button
              onClick={() => setIsCartOpen(true)}
              className="px-5 py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white font-bold text-xs shadow-lg shadow-emerald-600/30 transition-all hover:scale-105"
            >
              عرض السلة وتأكيد الطلب
            </button>
          </div>
        </div>
      )}

      {/* Cart Modal */}
      <PortalCartModal
        isOpen={isCartOpen}
        onClose={() => setIsCartOpen(false)}
        cart={cart}
        onUpdateQuantity={handleUpdateQuantity}
        onRemoveItem={handleRemoveItem}
        onClearCart={handleClearCart}
        onOrderSuccess={(orderNum) => {
          navigate('/portal/orders');
        }}
      />
    </div>
  );
};

export default PortalCatalog;
`;

fs.writeFileSync(cartModalPath, cartModalContent, 'utf8');
fs.writeFileSync(catalogPagePath, catalogPageContent, 'utf8');
console.log('PortalCartModal.tsx and PortalCatalog.tsx created.');

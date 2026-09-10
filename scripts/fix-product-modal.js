const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/features/products/components/ProductModal.tsx');

const content = `import React, { useEffect, useState, useMemo } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Package,
  X,
  Loader2,
  Plus,
  AlertTriangle,
  CheckCircle2,
  Store,
} from 'lucide-react';
import { productFormSchema, type ProductFormData } from '../types/products.schemas';
import { useCreateProductMutation, useUpdateProductMutation } from '../api/useProductsMutations';
import { CategoryUnitModal } from './CategoryUnitModal';
import type { Product, Category, Unit } from '../types/products.types';

interface ProductModalProps {
  isOpen: boolean;
  onClose: () => void;
  productToEdit?: Product | null;
  categories: Category[];
  units: Unit[];
}

export const ProductModal: React.FC<ProductModalProps> = ({
  isOpen,
  onClose,
  productToEdit,
  categories,
  units,
}) => {
  const [categoryModalMode, setCategoryModalMode] = useState<'category' | 'unit' | null>(null);

  const createMutation = useCreateProductMutation();
  const updateMutation = useUpdateProductMutation();

  const isEditing = !!productToEdit;
  const isPending = createMutation.isPending || updateMutation.isPending;

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    reset,
    formState: { errors },
  } = useForm<ProductFormData>({
    resolver: zodResolver(productFormSchema),
    defaultValues: {
      name: '',
      barcode: '',
      categoryId: '',
      unitId: '',
      purchaseCost: 0,
      sellingPrice: 0,
      minStockLevel: 5,
      initialStock: 0,
      wholesalePrice: null,
      isWholesaleAvailable: false,
    },
  });

  const watchCost = watch('purchaseCost') || 0;
  const watchPrice = watch('sellingPrice') || 0;
  const watchWholesalePrice = watch('wholesalePrice');
  const watchWholesaleAvailable = watch('isWholesaleAvailable');

  // Real-time Retail Profit Calculation
  const profitMargin = useMemo(() => {
    const profit = watchPrice - watchCost;
    const margin = watchPrice > 0 ? (profit / watchPrice) * 100 : 0;
    return {
      profit,
      margin,
      isLoss: profit < 0,
    };
  }, [watchCost, watchPrice]);

  // Wholesale Profit Calculation
  const wholesaleMargin = useMemo(() => {
    const effectivePrice =
      watchWholesalePrice && watchWholesalePrice > 0 ? watchWholesalePrice : watchPrice;
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

  useEffect(() => {
    if (isOpen) {
      if (productToEdit) {
        reset({
          name: productToEdit.name,
          barcode: productToEdit.barcode || '',
          categoryId: productToEdit.categoryId,
          unitId: productToEdit.unitId,
          purchaseCost: productToEdit.purchaseCost || 0,
          sellingPrice: productToEdit.sellingPrice || 0,
          minStockLevel: productToEdit.minStockLevel || 5,
          initialStock: productToEdit.currentStock || 0,
          wholesalePrice: productToEdit.wholesalePrice ?? null,
          isWholesaleAvailable: productToEdit.isWholesaleAvailable ?? false,
        });
      } else {
        reset({
          name: '',
          barcode: '',
          categoryId: categories[0]?.id || '',
          unitId: units[0]?.id || '',
          purchaseCost: 0,
          sellingPrice: 0,
          minStockLevel: 5,
          initialStock: 0,
          wholesalePrice: null,
          isWholesaleAvailable: false,
        });
      }
    }
  }, [isOpen, productToEdit, categories, units, reset]);

  if (!isOpen) return null;

  const onSubmit = async (data: ProductFormData) => {
    const wholesaleVal =
      data.wholesalePrice !== null && data.wholesalePrice !== undefined && data.wholesalePrice > 0
        ? Number(data.wholesalePrice)
        : undefined;

    if (isEditing && productToEdit) {
      await updateMutation.mutateAsync({
        id: productToEdit.id,
        payload: {
          name: data.name,
          barcode: data.barcode || undefined,
          categoryId: data.categoryId,
          unitId: data.unitId,
          purchaseCost: Number(data.purchaseCost),
          sellingPrice: Number(data.sellingPrice),
          minStockLevel: Number(data.minStockLevel),
          isWholesaleAvailable: Boolean(data.isWholesaleAvailable),
          wholesalePrice: wholesaleVal,
        },
      });
    } else {
      await createMutation.mutateAsync({
        name: data.name,
        barcode: data.barcode || undefined,
        categoryId: data.categoryId,
        unitId: data.unitId,
        purchaseCost: Number(data.purchaseCost),
        sellingPrice: Number(data.sellingPrice),
        minStockLevel: Number(data.minStockLevel),
        initialStock: Number(data.initialStock || 0),
        isWholesaleAvailable: Boolean(data.isWholesaleAvailable),
        wholesalePrice: wholesaleVal,
      });
    }
    onClose();
  };

  return (
    <>
      <div className="fixed inset-0 z-40 flex items-center justify-center p-4 bg-slate-900/70 backdrop-blur-sm animate-fade-in">
        <div className="bg-white dark:bg-slate-800 rounded-3xl border border-slate-200 dark:border-slate-700 shadow-2xl w-full max-w-xl overflow-hidden flex flex-col max-h-[90vh]">
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 dark:border-slate-700 bg-slate-50/50 dark:bg-slate-800/50">
            <div className="flex items-center gap-2.5">
              <div className="w-10 h-10 rounded-xl bg-emerald-600 text-white flex items-center justify-center shadow-md">
                <Package className="w-5 h-5" />
              </div>
              <div>
                <h3 className="font-extrabold text-slate-900 dark:text-white text-lg">
                  {isEditing ? 'تعديل بيانات الصنف' : 'إضافة صنف جديد'}
                </h3>
                <p className="text-xs text-slate-500">
                  {isEditing
                    ? 'تحديث الأسعار وتفاصيل الصنف وتوافر الجملة'
                    : 'إدخال صنف جديد لكتالوج المبيعات والمخزن وبوابة الجملة'}
                </p>
              </div>
            </div>

            <button
              type="button"
              onClick={onClose}
              className="w-8 h-8 rounded-lg flex items-center justify-center text-slate-400 hover:text-slate-600"
            >
              <X className="w-5 h-5" />
            </button>
          </div>

          {/* Form Body */}
          <form onSubmit={handleSubmit(onSubmit)} className="p-6 overflow-y-auto space-y-4 flex-1">
            {/* Name & Barcode */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 dark:text-slate-300 mb-1">
                  اسم الصنف *
                </label>
                <input
                  type="text"
                  {...register('name')}
                  placeholder="مثال: سكر الأسرة 1 كجم"
                  className="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-xs text-slate-900 dark:text-white focus:outline-none focus:border-emerald-500 font-bold"
                />
                {errors.name && (
                  <p className="text-[10px] text-rose-500 mt-1">{errors.name.message}</p>
                )}
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-700 dark:text-slate-300 mb-1">
                  الباركود الدولي / المحلي
                </label>
                <input
                  type="text"
                  {...register('barcode')}
                  placeholder="مثال: 6223000123456"
                  className="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-mono text-slate-900 dark:text-white focus:outline-none focus:border-emerald-500"
                />
                {errors.barcode && (
                  <p className="text-[10px] text-rose-500 mt-1">{errors.barcode.message}</p>
                )}
              </div>
            </div>

            {/* Category & Unit */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <div className="flex items-center justify-between mb-1">
                  <label className="text-xs font-bold text-slate-700 dark:text-slate-300">
                    القسم / الفئة *
                  </label>
                  <button
                    type="button"
                    onClick={() => setCategoryModalMode('category')}
                    className="text-[11px] text-emerald-600 dark:text-emerald-400 font-bold hover:underline flex items-center gap-0.5"
                  >
                    <Plus className="w-3 h-3" />
                    <span>قسم جديد</span>
                  </button>
                </div>
                <select
                  {...register('categoryId')}
                  className="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-semibold text-slate-800 dark:text-slate-200 focus:outline-none focus:border-emerald-500"
                >
                  <option value="">اختر القسم...</option>
                  {(Array.isArray(categories) ? categories : []).map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
                {errors.categoryId && (
                  <p className="text-[10px] text-rose-500 mt-1">{errors.categoryId.message}</p>
                )}
              </div>

              <div>
                <div className="flex items-center justify-between mb-1">
                  <label className="text-xs font-bold text-slate-700 dark:text-slate-300">
                    وحدة القياس *
                  </label>
                  <button
                    type="button"
                    onClick={() => setCategoryModalMode('unit')}
                    className="text-[11px] text-emerald-600 dark:text-emerald-400 font-bold hover:underline flex items-center gap-0.5"
                  >
                    <Plus className="w-3 h-3" />
                    <span>وحدة جديدة</span>
                  </button>
                </div>
                <select
                  {...register('unitId')}
                  className="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-semibold text-slate-800 dark:text-slate-200 focus:outline-none focus:border-emerald-500"
                >
                  <option value="">اختر الوحدة...</option>
                  {(Array.isArray(units) ? units : []).map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.name} ({u.symbol})
                    </option>
                  ))}
                </select>
                {errors.unitId && (
                  <p className="text-[10px] text-rose-500 mt-1">{errors.unitId.message}</p>
                )}
              </div>
            </div>

            {/* Financials: Cost, Selling Price & Live Profit Display */}
            <div className="p-4 rounded-2xl bg-slate-50 dark:bg-slate-900/60 border border-slate-200 dark:border-slate-700 space-y-3">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-bold text-slate-700 dark:text-slate-300 mb-1">
                    سعر التكلفة والشراء (ج.م) *
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    {...register('purchaseCost', { valueAsNumber: true })}
                    className="w-full px-3 py-2 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm font-bold font-mono text-slate-900 dark:text-white focus:outline-none focus:border-emerald-500"
                  />
                  {errors.purchaseCost && (
                    <p className="text-[10px] text-rose-500 mt-1">{errors.purchaseCost.message}</p>
                  )}
                </div>

                <div>
                  <label className="block text-xs font-bold text-slate-700 dark:text-slate-300 mb-1">
                    سعر البيع للمستهلك القطاعي (ج.م) *
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    {...register('sellingPrice', { valueAsNumber: true })}
                    className="w-full px-3 py-2 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm font-bold font-mono text-slate-900 dark:text-white focus:outline-none focus:border-emerald-500"
                  />
                  {errors.sellingPrice && (
                    <p className="text-[10px] text-rose-500 mt-1">{errors.sellingPrice.message}</p>
                  )}
                </div>
              </div>

              {/* Retail Profit Indicator Card */}
              <div
                className={\`p-3 rounded-xl flex items-center justify-between border text-xs font-bold \${
                  profitMargin.isLoss
                    ? 'bg-rose-50 dark:bg-rose-950/40 border-rose-200 dark:border-rose-800 text-rose-800 dark:text-rose-300'
                    : 'bg-emerald-50 dark:bg-emerald-950/40 border-emerald-200 dark:border-emerald-800 text-emerald-800 dark:text-emerald-300'
                }\`}
              >
                <span>
                  {profitMargin.isLoss ? '⚠️ تحذير: هامش ربح قطاعي سالب (خسارة)' : 'ربح القطعة قطاعي:'}
                </span>
                <span className="font-mono text-sm">
                  {profitMargin.profit.toFixed(2)} ج.م ({profitMargin.margin.toFixed(1)}%)
                </span>
              </div>
            </div>

            {/* Wholesale Configuration Card */}
            <div className="p-4 rounded-2xl bg-indigo-50/60 dark:bg-indigo-950/30 border border-indigo-200 dark:border-indigo-800/60 space-y-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div className="p-1.5 rounded-lg bg-indigo-600 text-white">
                    <Store className="w-4 h-4" />
                  </div>
                  <div>
                    <span className="text-xs font-bold text-slate-900 dark:text-white block">
                      إتاحة الصنف في بوابة الجملة (B2B)
                    </span>
                    <span className="text-[11px] text-slate-500 dark:text-slate-400">
                      يظهر لتجار الجملة المعتمدين للشراء الذاتي
                    </span>
                  </div>
                </div>

                <label className="relative inline-flex items-center cursor-pointer">
                  <input
                    type="checkbox"
                    {...register('isWholesaleAvailable')}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-slate-200 peer-focus:outline-none rounded-full peer dark:bg-slate-700 peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
                </label>
              </div>

              {watchWholesaleAvailable && (
                <div className="pt-2 space-y-3 border-t border-indigo-100 dark:border-indigo-900/50 animate-in fade-in duration-200">
                  <div>
                    <label className="block text-xs font-bold text-slate-700 dark:text-slate-300 mb-1">
                      سعر البيع بالجملة للتجار (ج.م)
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      {...register('wholesalePrice', {
                        setValueAs: (v) => (v === '' || v === null || v === undefined || isNaN(Number(v)) ? null : Number(v)),
                      })}
                      placeholder={\`اتركه فارغاً لاعتماد سعر المستهلك (\${watchPrice || 0} ج.م)\`}
                      className="w-full px-3 py-2 bg-white dark:bg-slate-800 border border-indigo-300 dark:border-indigo-700 rounded-xl text-sm font-bold font-mono text-slate-900 dark:text-white focus:outline-none focus:border-indigo-500"
                    />
                    <p className="text-[10px] text-indigo-600 dark:text-indigo-400 mt-1">
                      {wholesaleMargin.isFallback
                        ? \`يتم تطبيق سعر البيع الافتراضي للمستهلك (\${watchPrice || 0} ج.م) لعدم تحديد سعر جملة خاص.\`
                        : \`سعر الجملة المعتمد للتاجر: \${wholesaleMargin.effectivePrice} ج.م\`}
                    </p>
                  </div>

                  {/* Wholesale Margin Indicator */}
                  <div
                    className={\`p-2.5 rounded-xl flex items-center justify-between border text-xs font-bold \${
                      wholesaleMargin.isLoss
                        ? 'bg-rose-50 dark:bg-rose-950/40 border-rose-200 dark:border-rose-800 text-rose-800 dark:text-rose-300'
                        : 'bg-indigo-100/70 dark:bg-indigo-900/40 border-indigo-200 dark:border-indigo-800 text-indigo-900 dark:text-indigo-200'
                    }\`}
                  >
                    <span>
                      {wholesaleMargin.isLoss
                        ? '⚠️ تحذير: سعر الجملة أقل من سعر التكلفة (خسارة)!'
                        : 'هامش ربح القطعة بسعر الجملة:'}
                    </span>
                    <span className="font-mono text-xs">
                      {wholesaleMargin.profit.toFixed(2)} ج.م ({wholesaleMargin.margin.toFixed(1)}%)
                    </span>
                  </div>
                </div>
              )}
            </div>

            {/* Stock Parameters */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-bold text-slate-700 dark:text-slate-300 mb-1">
                  حد الطلب الأدنى للتنبيه
                </label>
                <input
                  type="number"
                  min="0"
                  {...register('minStockLevel', { valueAsNumber: true })}
                  className="w-full px-3 py-2 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-mono text-slate-900 dark:text-white focus:outline-none focus:border-emerald-500"
                />
              </div>

              {!isEditing && (
                <div>
                  <label className="block text-xs font-bold text-slate-700 dark:text-slate-300 mb-1">
                    الرصيد الافتتاحي بالمخزن
                  </label>
                  <input
                    type="number"
                    min="0"
                    {...register('initialStock', { valueAsNumber: true })}
                    className="w-full px-3 py-2 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-mono text-slate-900 dark:text-white focus:outline-none focus:border-emerald-500"
                  />
                </div>
              )}
            </div>

            {/* Global Errors summary if any */}
            {Object.keys(errors).length > 0 && (
              <div className="p-3 bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-800 rounded-xl text-xs text-rose-700 dark:text-rose-300 flex items-center gap-2">
                <AlertTriangle className="w-4 h-4 flex-shrink-0" />
                <span>يرجى استكمال الحقول المطلوبة بشكل صحيح قبل الحفظ.</span>
              </div>
            )}

            {/* Actions */}
            <div className="pt-3 flex items-center justify-end gap-2 border-t border-slate-100 dark:border-slate-700">
              <button
                type="button"
                onClick={onClose}
                className="px-4 py-2 text-xs font-bold text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-xl transition-colors"
              >
                إلغاء
              </button>
              <button
                type="submit"
                disabled={isPending}
                className="flex items-center gap-1.5 px-6 py-2.5 text-xs font-extrabold text-white bg-emerald-600 hover:bg-emerald-700 rounded-xl shadow-md disabled:opacity-40"
              >
                {isPending ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <CheckCircle2 className="w-4 h-4" />
                )}
                <span>{isEditing ? 'حفظ التعديلات' : 'إضافة الصنف'}</span>
              </button>
            </div>
          </form>
        </div>
      </div>

      {/* Sub-modal for Quick Category/Unit */}
      <CategoryUnitModal
        isOpen={categoryModalMode !== null}
        onClose={() => setCategoryModalMode(null)}
        mode={categoryModalMode || 'category'}
        onSuccess={(id) => {
          if (categoryModalMode === 'category') setValue('categoryId', id);
          if (categoryModalMode === 'unit') setValue('unitId', id);
        }}
      />
    </>
  );
};

export default ProductModal;
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('ProductModal.tsx rewritten with valueAsNumber.');

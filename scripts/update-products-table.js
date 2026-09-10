const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/features/products/components/ProductsTable.tsx');

const content = `import React from 'react';
import { Edit, Trash2, Tag, ChevronRight, ChevronLeft, AlertTriangle, XCircle, CheckCircle, Store } from 'lucide-react';
import type { Product } from '../types/products.types';

interface ProductsTableProps {
  products: Product[];
  isLoading: boolean;
  totalCount: number;
  currentPage: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onEdit: (product: Product) => void;
  onDelete: (product: Product) => void;
  onToggleStatus: (product: Product) => void;
  canManage: boolean;
}

export const ProductsTable: React.FC<ProductsTableProps> = ({
  products,
  isLoading,
  totalCount,
  currentPage,
  pageSize,
  onPageChange,
  onEdit,
  onDelete,
  onToggleStatus,
  canManage,
}) => {
  const totalPages = Math.ceil(totalCount / pageSize) || 1;

  if (isLoading) {
    return (
      <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200/80 dark:border-slate-700/80 p-8 text-center">
        <div className="space-y-3 max-w-lg mx-auto">
          {[...Array(5)].map((_, i) => (
            <div key={i} className="h-12 rounded-xl bg-slate-100 dark:bg-slate-700/50 animate-pulse" />
          ))}
        </div>
      </div>
    );
  }

  if (products.length === 0) {
    return (
      <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200/80 dark:border-slate-700/80 p-12 text-center text-slate-600 dark:text-slate-300 font-medium">
        <Tag className="w-12 h-12 stroke-[1.2] mx-auto mb-2 opacity-50 text-slate-400" />
        <h4 className="font-bold text-slate-700 dark:text-slate-200 text-sm">لا توجد أصناف مطابقة</h4>
        <p className="text-xs text-slate-400 mt-1">جرب تغيير معايير البحث أو إضافة صنف جديد</p>
      </div>
    );
  }

  return (
    <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200/80 dark:border-slate-700/80 shadow-sm overflow-hidden flex flex-col">
      <div className="overflow-x-auto">
        <table className="w-full text-right text-xs">
          <thead>
            <tr className="bg-slate-100 dark:bg-slate-900 border-b border-slate-200 dark:border-slate-700 font-extrabold text-slate-800 dark:text-slate-100">
              <th className="py-3.5 px-4">الصنف / الباركود</th>
              <th className="py-3.5 px-3">القسم والوحدة</th>
              <th className="py-3.5 px-3 text-center">سعر التكلفة</th>
              <th className="py-3.5 px-3 text-center">سعر البيع قطاعي</th>
              <th className="py-3.5 px-3 text-center">سعر الجملة (B2B)</th>
              <th className="py-3.5 px-3 text-center">هامش الربح قطاعي</th>
              <th className="py-3.5 px-3 text-center">المخزون الحالي</th>
              <th className="py-3.5 px-3 text-center">الحالة</th>
              {canManage && <th className="py-3.5 px-4 text-left">إجراءات</th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-700/60">
            {products.map((p) => {
              const cost = p.purchaseCost || 0;
              const price = p.sellingPrice || 0;
              const profit = price - cost;
              const marginPercent = price > 0 ? (profit / price) * 100 : 0;
              const stock = p.currentStock || 0;
              const minStock = p.minStockLevel || 0;
              const isOutOfStock = stock <= 0;
              const isLowStock = stock > 0 && stock <= minStock;

              return (
                <tr
                  key={p.id}
                  className="hover:bg-slate-100/70 dark:hover:bg-slate-700/60 transition-colors"
                >
                  {/* Name & Barcode */}
                  <td className="py-3.5 px-4">
                    <div className="font-bold text-slate-900 dark:text-white text-sm">
                      {p.name}
                    </div>
                    {p.barcode ? (
                      <span className="font-mono text-[11px] text-slate-600 dark:text-slate-300 font-medium">{p.barcode}</span>
                    ) : (
                      <span className="text-[10px] text-slate-400">بدون باركود</span>
                    )}
                  </td>

                  {/* Category & Unit */}
                  <td className="py-3.5 px-3">
                    <span className="px-2 py-0.5 rounded-md bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-200 font-semibold text-[11px]">
                      {p.categoryName || 'عام'}
                    </span>
                    <span className="block text-[10px] text-slate-600 dark:text-slate-300 mt-1 font-semibold">
                      الوحدة: {p.unitName || 'قطعة'}
                    </span>
                  </td>

                  {/* Purchase Cost */}
                  <td className="py-3.5 px-3 text-center font-mono font-bold text-slate-600 dark:text-slate-300">
                    {cost.toFixed(2)} ج.م
                  </td>

                  {/* Selling Price */}
                  <td className="py-3.5 px-3 text-center font-mono font-black text-slate-900 dark:text-white text-sm">
                    {price.toFixed(2)} ج.م
                  </td>

                  {/* Wholesale Price (B2B) */}
                  <td className="py-3.5 px-3 text-center">
                    {p.isWholesaleAvailable ? (
                      <div>
                        <span className="font-mono font-black text-indigo-600 dark:text-indigo-400 text-sm block">
                          {(p.wholesalePrice && p.wholesalePrice > 0 ? p.wholesalePrice : price).toFixed(2)} ج.م
                        </span>
                        <span className="inline-flex items-center gap-0.5 text-[10px] text-indigo-600 dark:text-indigo-400 font-bold bg-indigo-50 dark:bg-indigo-950/50 px-1.5 py-0.5 rounded">
                          <Store className="w-2.5 h-2.5" />
                          <span>{p.wholesalePrice ? 'سعر خاص' : 'سعر المستهلك'}</span>
                        </span>
                      </div>
                    ) : (
                      <span className="px-2 py-0.5 rounded text-[11px] font-semibold text-slate-400 bg-slate-100 dark:bg-slate-750">
                        غير متاح
                      </span>
                    )}
                  </td>

                  {/* Margin % */}
                  <td className="py-3.5 px-3 text-center">
                    <div
                      className={\`inline-flex items-center gap-0.5 px-2 py-0.5 rounded-md font-mono text-[11px] font-bold \${
                        profit <= 0
                          ? 'bg-rose-50 dark:bg-rose-950/40 text-rose-600'
                          : 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600'
                      }\`}
                    >
                      <span>{profit.toFixed(2)} ج.م</span>
                      <span className="text-[10px] opacity-80">({marginPercent.toFixed(1)}%)</span>
                    </div>
                  </td>

                  {/* Stock Level */}
                  <td className="py-3.5 px-3 text-center">
                    <span
                      className={\`inline-flex items-center gap-1 px-2.5 py-1 rounded-lg font-mono font-bold text-xs \${
                        isOutOfStock
                          ? 'bg-rose-100 dark:bg-rose-950/60 text-rose-700 dark:text-rose-300'
                          : isLowStock
                          ? 'bg-amber-100 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300'
                          : 'bg-emerald-100 dark:bg-emerald-950/60 text-emerald-700 dark:text-emerald-300'
                      }\`}
                    >
                      {isOutOfStock && <XCircle className="w-3.5 h-3.5" />}
                      {isLowStock && <AlertTriangle className="w-3.5 h-3.5" />}
                      {!isOutOfStock && !isLowStock && <CheckCircle className="w-3.5 h-3.5" />}
                      <span>{stock}</span>
                    </span>
                    <span className="block text-[10px] text-slate-600 dark:text-slate-300 mt-0.5 font-semibold">حد الطلب: {minStock}</span>
                  </td>

                  {/* Status Toggle Switch */}
                  <td className="py-3.5 px-3 text-center">
                    {canManage ? (
                      <button
                        type="button"
                        onClick={() => onToggleStatus(p)}
                        className={\`px-2.5 py-1 rounded-lg font-bold text-[11px] transition-all \${
                          p.isActive
                            ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300 hover:bg-emerald-100'
                            : 'bg-slate-100 text-slate-500 dark:bg-slate-700 dark:text-slate-400 hover:bg-slate-200'
                        }\`}
                      >
                        {p.isActive ? 'نشط' : 'معطل'}
                      </button>
                    ) : (
                      <span className={\`text-xs font-bold \${p.isActive ? 'text-emerald-600' : 'text-slate-500 dark:text-slate-400 font-bold'}\`}>
                        {p.isActive ? 'نشط' : 'معطل'}
                      </span>
                    )}
                  </td>

                  {/* Actions */}
                  {canManage && (
                    <td className="py-3.5 px-4 text-left">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          type="button"
                          onClick={() => onEdit(p)}
                          className="p-1.5 text-slate-600 dark:text-slate-300 hover:text-emerald-600 dark:hover:text-emerald-400 hover:bg-emerald-50 dark:hover:bg-emerald-950/40 rounded-lg transition-colors"
                          title="تعديل بيانات الصنف"
                        >
                          <Edit className="w-4 h-4" />
                        </button>

                        <button
                          type="button"
                          onClick={() => onDelete(p)}
                          className="p-1.5 text-slate-600 dark:text-slate-300 hover:text-rose-600 dark:hover:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/40 rounded-lg transition-colors"
                          title="حذف الصنف"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  )}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Pagination Footer */}
      {totalPages > 1 && (
        <div className="p-4 border-t border-slate-100 dark:border-slate-700/80 bg-slate-50/50 dark:bg-slate-850/50 flex items-center justify-between">
          <span className="text-xs text-slate-700 dark:text-slate-200 font-bold">
            صفحة <b className="text-slate-800 dark:text-white">{currentPage}</b> من{' '}
            <b className="text-slate-800 dark:text-white">{totalPages}</b> (إجمالي {totalCount} صنف)
          </span>

          <div className="flex items-center gap-1.5">
            <button
              type="button"
              onClick={() => onPageChange(currentPage - 1)}
              disabled={currentPage <= 1}
              className="p-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-slate-100"
            >
              <ChevronRight className="w-4 h-4" />
            </button>

            <button
              type="button"
              onClick={() => onPageChange(currentPage + 1)}
              disabled={currentPage >= totalPages}
              className="p-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-slate-100"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default ProductsTable;
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('ProductsTable.tsx updated with wholesale price column.');

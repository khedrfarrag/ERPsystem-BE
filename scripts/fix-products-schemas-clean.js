const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/features/products/types/products.schemas.ts');

const content = `import { z } from 'zod';

export const productFormSchema = z.object({
  name: z.string().min(2, 'اسم الصنف يجب أن يتكون من حرفين على الأقل'),
  barcode: z.string().optional().nullable(),
  categoryId: z.string().min(1, 'يرجى اختيار القسم'),
  unitId: z.string().min(1, 'يرجى اختيار وحدة القياس'),
  purchaseCost: z.number().min(0, 'سعر التكلفة يجب أن يكون 0 أو أكثر'),
  sellingPrice: z.number().min(0.01, 'سعر البيع يجب أن يكون أكبر من 0'),
  minStockLevel: z.number().min(0, 'حد الطلب الأدنى يجب أن يكون 0 أو أكثر'),
  wholesalePrice: z.number().min(0, 'سعر الجملة لا يمكن أن يكون سالباً').optional().nullable(),
  isWholesaleAvailable: z.boolean(),
  initialStock: z.number().min(0, 'الرصيد الافتتاحي لا يمكن أن يكون سالباً').optional(),
});

export type ProductFormData = z.infer<typeof productFormSchema>;

export const categoryFormSchema = z.object({
  name: z.string().min(2, 'اسم القسم مطلوب (حرفين على الأقل)'),
  description: z.string().optional().nullable(),
});

export type CategoryFormData = z.infer<typeof categoryFormSchema>;

export const unitFormSchema = z.object({
  name: z.string().min(1, 'اسم الوحدة مطلوب'),
  symbol: z.string().min(1, 'رمز الوحدة مطلوب (مثال: قطعة، كجم، لتر)'),
});

export type UnitFormData = z.infer<typeof unitFormSchema>;
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('products.schemas.ts restored with clean types.');

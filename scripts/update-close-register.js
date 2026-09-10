const fs = require('fs');
const filePath = 'G:/system-analysiss-saas/system-FE/src/features/expenses/components/CloseRegisterModal.tsx';
let content = fs.readFileSync(filePath, 'utf8');

// Ensure toast is imported if not already
if (!content.includes('import toast')) {
  content = `import toast from 'react-hot-toast';\n` + content;
}

// 1. Update onSubmit to notify drawer zeroing
const targetOnSubmit = /const onSubmit = async \(data: CloseRegisterFormValues\) => \{[\s\S]*?reset\(\);\s*onClose\(\);\s*\};/;
const newOnSubmit = `const onSubmit = async (data: CloseRegisterFormValues) => {
    await closeRegisterMutation.mutateAsync({
      countedAmount: data.countedAmount,
      notes: data.notes?.trim() || null,
    });
    toast.success('تم إغلاق الوردية وتوريد النقدية وتصفير الدرج بنجاح!');
    reset();
    onClose();
  };`;
content = content.replace(targetOnSubmit, newOnSubmit);

// 2. Add Sweep notification card before Actions
const targetActions = /\{\/\* Actions \*\/\}/;
const sweepCard = `{/* Cash Drawer Sweep Info */}
          <div className="p-3 bg-blue-50 dark:bg-blue-950/40 border border-blue-200 dark:border-blue-800 rounded-2xl text-[11px] text-blue-800 dark:text-blue-300 flex items-start gap-2">
            <span className="font-bold">ℹ️ تنبيه التوريد:</span>
            <span>عند تأكيد الإغلاق، سيتم تسوية الفارق، وتوريد كامل المبلغ الفعلي ({countedAmount.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م) للخزينة وتصفير الدرج ليصبح (0.00 ج.م) لبدء الوردية القادمة.</span>
          </div>

          {/* Actions */}`;

content = content.replace(targetActions, sweepCard);

fs.writeFileSync(filePath, content, 'utf8');
console.log('CloseRegisterModal updated successfully!');

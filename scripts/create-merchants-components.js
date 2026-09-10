const fs = require('fs');
const path = require('path');

const tablePath = path.resolve(__dirname, '../../system-FE/src/features/b2b/components/MerchantsTable.tsx');
const modalPath = path.resolve(__dirname, '../../system-FE/src/features/b2b/components/CreateMerchantModal.tsx');

const tableContent = `import React from 'react';
import { Merchant } from '../types/b2b.types';
import { 
  Building2, 
  Phone, 
  Mail, 
  MapPin, 
  CreditCard, 
  Edit3, 
  Power, 
  MessageCircle,
  AlertTriangle,
  CheckCircle2,
  Clock
} from 'lucide-react';
import { buildWhatsAppUrl } from '../utils/whatsappUtils';

interface MerchantsTableProps {
  merchants: Merchant[];
  isLoading: boolean;
  onEdit: (merchant: Merchant) => void;
  onToggleActive: (id: string) => void;
}

export const MerchantsTable: React.FC<MerchantsTableProps> = ({
  merchants,
  isLoading,
  onEdit,
  onToggleActive,
}) => {
  if (isLoading) {
    return (
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-8 text-center text-slate-400 animate-pulse">
        جاري تحميل بيانات تجار الجملة...
      </div>
    );
  }

  if (merchants.length === 0) {
    return (
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-12 text-center">
        <Building2 className="w-12 h-12 text-slate-600 mx-auto mb-3" />
        <h3 className="text-lg font-bold text-white mb-1">لا يوجد تجار جملة مسجلين</h3>
        <p className="text-sm text-slate-400">ابدأ بإضافة أول متجر أو تاجر جملة عبر زر "إضافة تاجر جملة جديد".</p>
      </div>
    );
  }

  return (
    <div className="bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden shadow-xl">
      <div className="overflow-x-auto">
        <table className="w-full text-right text-sm text-slate-300">
          <thead className="bg-slate-950/70 text-slate-400 text-xs font-bold uppercase border-b border-slate-800">
            <tr>
              <th className="px-6 py-4">المتجر والمسئول</th>
              <th className="px-6 py-4">الاتصال</th>
              <th className="px-6 py-4">سقف الائتمان</th>
              <th className="px-6 py-4">الرصيد القائم</th>
              <th className="px-6 py-4">شروط الدفع</th>
              <th className="px-6 py-4">الحالة</th>
              <th className="px-6 py-4 text-center">إجراءات</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800/60">
            {merchants.map((merchant) => {
              const isOverLimit = merchant.creditLimit > 0 && merchant.currentBalance > merchant.creditLimit;
              const waUrl = buildWhatsAppUrl(
                merchant.phone,
                \`السلام عليكم ورحمة الله، مرحباً \${merchant.contactPerson} من طرف إدارة التوريد بالجملة.\`
              );

              return (
                <tr key={merchant.id} className="hover:bg-slate-800/40 transition-colors">
                  {/* Shop & Contact */}
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-primary-600 to-indigo-600 flex items-center justify-center text-white font-bold flex-shrink-0 shadow-sm">
                        <Building2 className="w-5 h-5" />
                      </div>
                      <div>
                        <div className="font-bold text-white text-base">{merchant.tradeName}</div>
                        <div className="text-xs text-slate-400 flex items-center gap-1.5 mt-0.5">
                          <span>{merchant.contactPerson}</span>
                          {merchant.address && (
                            <>
                              <span>•</span>
                              <span className="flex items-center gap-0.5 text-slate-500 truncate max-w-[160px]">
                                <MapPin className="w-3 h-3" />
                                {merchant.address}
                              </span>
                            </>
                          )}
                        </div>
                      </div>
                    </div>
                  </td>

                  {/* Contact Phone & WhatsApp */}
                  <td className="px-6 py-4">
                    <div className="flex flex-col gap-1">
                      <div className="flex items-center gap-2">
                        <span className="font-mono text-slate-200 text-xs" dir="ltr">{merchant.phone}</span>
                        <a
                          href={waUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="p-1 rounded-md bg-emerald-500/10 text-emerald-400 hover:bg-emerald-500/20 transition"
                          title="فتح محادثة واتساب"
                        >
                          <MessageCircle className="w-3.5 h-3.5" />
                        </a>
                      </div>
                      {merchant.email && (
                        <div className="text-xs text-slate-400 flex items-center gap-1">
                          <Mail className="w-3 h-3" />
                          <span className="truncate max-w-[150px]">{merchant.email}</span>
                        </div>
                      )}
                    </div>
                  </td>

                  {/* Credit Limit */}
                  <td className="px-6 py-4 font-mono font-medium text-slate-200">
                    {merchant.creditLimit.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                  </td>

                  {/* Current Balance */}
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-1.5">
                      <span className={\`font-mono font-bold \${
                        isOverLimit ? 'text-rose-400' : merchant.currentBalance > 0 ? 'text-amber-400' : 'text-emerald-400'
                      }\`}>
                        {merchant.currentBalance.toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م
                      </span>
                      {isOverLimit && (
                        <span title="تجاوز سقف الائتمان" className="text-rose-400">
                          <AlertTriangle className="w-4 h-4 inline" />
                        </span>
                      )}
                    </div>
                  </td>

                  {/* Payment Terms */}
                  <td className="px-6 py-4 text-xs text-slate-300">
                    <div className="flex items-center gap-1 bg-slate-800 px-2.5 py-1 rounded-lg w-fit">
                      <Clock className="w-3 h-3 text-slate-400" />
                      <span>{merchant.paymentTerms || 'افتراضي'}</span>
                    </div>
                  </td>

                  {/* Status */}
                  <td className="px-6 py-4">
                    <button
                      onClick={() => onToggleActive(merchant.id)}
                      className={\`px-3 py-1 rounded-full text-xs font-bold inline-flex items-center gap-1 transition-all \${
                        merchant.isActive
                          ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 hover:bg-emerald-500/20'
                          : 'bg-slate-800 text-slate-400 border border-slate-700 hover:bg-slate-700'
                      }\`}
                    >
                      {merchant.isActive ? (
                        <>
                          <CheckCircle2 className="w-3.5 h-3.5" />
                          <span>نشط</span>
                        </>
                      ) : (
                        <>
                          <Power className="w-3.5 h-3.5" />
                          <span>معطل</span>
                        </>
                      )}
                    </button>
                  </td>

                  {/* Actions */}
                  <td className="px-6 py-4 text-center">
                    <button
                      onClick={() => onEdit(merchant)}
                      className="p-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white transition"
                      title="تعديل بيانات التاجر"
                    >
                      <Edit3 className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};
`;

const modalContent = `import React, { useState, useEffect } from 'react';
import { Merchant, CreateMerchantRequest, UpdateMerchantRequest } from '../types/b2b.types';
import { X, Building2, User, Phone, Mail, MapPin, CreditCard, Lock, Clock } from 'lucide-react';

interface CreateMerchantModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmitCreate: (data: CreateMerchantRequest) => Promise<any>;
  onSubmitUpdate: (id: string, data: UpdateMerchantRequest) => Promise<any>;
  merchantToEdit: Merchant | null;
}

export const CreateMerchantModal: React.FC<CreateMerchantModalProps> = ({
  isOpen,
  onClose,
  onSubmitCreate,
  onSubmitUpdate,
  merchantToEdit,
}) => {
  const [formData, setFormData] = useState({
    tradeName: '',
    contactPerson: '',
    phone: '',
    email: '',
    password: '',
    address: '',
    creditLimit: '0',
    paymentTerms: 'Net 15',
    isActive: true,
  });

  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (merchantToEdit) {
      setFormData({
        tradeName: merchantToEdit.tradeName,
        contactPerson: merchantToEdit.contactPerson,
        phone: merchantToEdit.phone,
        email: merchantToEdit.email || '',
        password: '',
        address: merchantToEdit.address || '',
        creditLimit: merchantToEdit.creditLimit.toString(),
        paymentTerms: merchantToEdit.paymentTerms || 'Net 15',
        isActive: merchantToEdit.isActive,
      });
    } else {
      setFormData({
        tradeName: '',
        contactPerson: '',
        phone: '',
        email: '',
        password: '',
        address: '',
        creditLimit: '0',
        paymentTerms: 'Net 15',
        isActive: true,
      });
    }
  }, [merchantToEdit, isOpen]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    try {
      if (merchantToEdit) {
        await onSubmitUpdate(merchantToEdit.id, {
          tradeName: formData.tradeName,
          contactPerson: formData.contactPerson,
          phone: formData.phone,
          email: formData.email || undefined,
          address: formData.address || undefined,
          creditLimit: parseFloat(formData.creditLimit) || 0,
          paymentTerms: formData.paymentTerms || undefined,
          isActive: formData.isActive,
        });
      } else {
        await onSubmitCreate({
          tradeName: formData.tradeName,
          contactPerson: formData.contactPerson,
          phone: formData.phone,
          email: formData.email || undefined,
          password: formData.password,
          address: formData.address || undefined,
          creditLimit: parseFloat(formData.creditLimit) || 0,
          paymentTerms: formData.paymentTerms || undefined,
        });
      }
      onClose();
    } catch (err) {
      // Error handled by mutation toast
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm animate-fadeIn" dir="rtl">
      <div className="bg-slate-900 border border-slate-800 rounded-3xl max-w-xl w-full p-6 shadow-2xl relative max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between pb-4 border-b border-slate-800">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-primary-600/20 text-primary-400 flex items-center justify-center font-bold">
              <Building2 className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-white">
                {merchantToEdit ? 'تعديل بيانات تاجر جملة' : 'تسجيل تاجر جملة جديد'}
              </h2>
              <p className="text-xs text-slate-400">
                {merchantToEdit ? 'تحديث حدود الائتمان وبيانات الاتصال' : 'إنشاء حساب تاجر وحساب بوابة التوريد'}
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

        {/* Form */}
        <form onSubmit={handleSubmit} className="mt-6 space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Trade Name */}
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">
                اسم المتجر / الاسم التجاري <span className="text-rose-500">*</span>
              </label>
              <div className="relative">
                <Building2 className="w-4 h-4 absolute right-3 top-3 text-slate-400" />
                <input
                  type="text"
                  required
                  value={formData.tradeName}
                  onChange={(e) => setFormData({ ...formData, tradeName: e.target.value })}
                  placeholder="مثال: سوبرماركت الأمل"
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl py-2.5 pr-10 pl-3 text-sm text-white focus:outline-none focus:border-primary-500 transition"
                />
              </div>
            </div>

            {/* Contact Person */}
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">
                اسم المسئول / التاجر <span className="text-rose-500">*</span>
              </label>
              <div className="relative">
                <User className="w-4 h-4 absolute right-3 top-3 text-slate-400" />
                <input
                  type="text"
                  required
                  value={formData.contactPerson}
                  onChange={(e) => setFormData({ ...formData, contactPerson: e.target.value })}
                  placeholder="مثال: محمود أحمد"
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl py-2.5 pr-10 pl-3 text-sm text-white focus:outline-none focus:border-primary-500 transition"
                />
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Phone */}
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">
                رقم الهاتف والواتساب <span className="text-rose-500">*</span>
              </label>
              <div className="relative">
                <Phone className="w-4 h-4 absolute right-3 top-3 text-slate-400" />
                <input
                  type="tel"
                  required
                  value={formData.phone}
                  onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                  placeholder="مثال: +201012345678"
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl py-2.5 pr-10 pl-3 text-sm text-white font-mono focus:outline-none focus:border-primary-500 transition"
                  dir="ltr"
                />
              </div>
            </div>

            {/* Email */}
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">
                البريد الإلكتروني (اختياري)
              </label>
              <div className="relative">
                <Mail className="w-4 h-4 absolute right-3 top-3 text-slate-400" />
                <input
                  type="email"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  placeholder="merchant@example.com"
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl py-2.5 pr-10 pl-3 text-sm text-white focus:outline-none focus:border-primary-500 transition"
                />
              </div>
            </div>
          </div>

          {!merchantToEdit && (
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">
                كلمة مرور بوابة التاجر <span className="text-rose-500">*</span>
              </label>
              <div className="relative">
                <Lock className="w-4 h-4 absolute right-3 top-3 text-slate-400" />
                <input
                  type="password"
                  required
                  minLength={6}
                  value={formData.password}
                  onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                  placeholder="لا تقل عن 6 أحرف"
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl py-2.5 pr-10 pl-3 text-sm text-white focus:outline-none focus:border-primary-500 transition"
                />
              </div>
              <p className="text-[11px] text-slate-500 mt-1">سيستخدم التاجر بريده الإلكتروني أو رقم هاتفه وهذه الكلمة لتسجيل الدخول للبوابة.</p>
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Credit Limit */}
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">
                سقف الائتمان المسموح به (ج.م)
              </label>
              <div className="relative">
                <CreditCard className="w-4 h-4 absolute right-3 top-3 text-slate-400" />
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  value={formData.creditLimit}
                  onChange={(e) => setFormData({ ...formData, creditLimit: e.target.value })}
                  placeholder="0.00"
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl py-2.5 pr-10 pl-3 text-sm text-white font-mono focus:outline-none focus:border-primary-500 transition"
                />
              </div>
            </div>

            {/* Payment Terms */}
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">
                شروط الدفع المتفق عليها
              </label>
              <div className="relative">
                <Clock className="w-4 h-4 absolute right-3 top-3 text-slate-400" />
                <input
                  type="text"
                  value={formData.paymentTerms}
                  onChange={(e) => setFormData({ ...formData, paymentTerms: e.target.value })}
                  placeholder="مثال: Net 15, Net 30, نقداً عند الاستلام"
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl py-2.5 pr-10 pl-3 text-sm text-white focus:outline-none focus:border-primary-500 transition"
                />
              </div>
            </div>
          </div>

          {/* Address */}
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1">
              عنوان التوصيل / المتجر
            </label>
            <div className="relative">
              <MapPin className="w-4 h-4 absolute right-3 top-3 text-slate-400" />
              <input
                type="text"
                value={formData.address}
                onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                placeholder="مثال: القاهرة، شارع التحرير، عمارة 14"
                className="w-full bg-slate-950 border border-slate-800 rounded-xl py-2.5 pr-10 pl-3 text-sm text-white focus:outline-none focus:border-primary-500 transition"
              />
            </div>
          </div>

          {/* Action buttons */}
          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-800 mt-6">
            <button
              type="button"
              onClick={onClose}
              className="px-5 py-2.5 rounded-xl border border-slate-700 text-sm font-semibold text-slate-300 hover:bg-slate-800 transition"
            >
              إلغاء
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-6 py-2.5 rounded-xl bg-primary-600 hover:bg-primary-500 text-sm font-semibold text-white shadow-lg shadow-primary-600/30 transition disabled:opacity-50"
            >
              {isSubmitting ? 'جاري الحفظ...' : merchantToEdit ? 'تحديث البيانات' : 'تسجيل التاجر'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
`;

fs.writeFileSync(tablePath, tableContent, 'utf8');
fs.writeFileSync(modalPath, modalContent, 'utf8');
console.log('MerchantsTable.tsx and CreateMerchantModal.tsx created.');

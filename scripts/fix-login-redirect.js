const fs = require('fs');
const path = require('path');

const targetPath = path.resolve(__dirname, '../../system-FE/src/pages/Login.tsx');

const content = `import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useAuth } from '../context/AuthContext';
import api from '../api/client';
import type { ApiResponse, AuthResponse } from '../types';
import { loginSchema, type LoginFormData } from '../lib/validations';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Lock, Mail, Sparkles, ShieldCheck, Sun, Moon } from 'lucide-react';
import { useTheme } from '../context/ThemeContext';
import toast from 'react-hot-toast';

export const Login: React.FC = () => {
  const { theme, toggleTheme } = useTheme();
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [loading, setLoading] = useState(false);

  const from = (location.state as any)?.from?.pathname || '/';

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  });

  const onSubmit = async (data: LoginFormData) => {
    try {
      setLoading(true);
      const res = await api.post<ApiResponse<AuthResponse>>('/auth/login', data);

      if (res.data.success && res.data.data) {
        const loggedUser = res.data.data.user;
        login(res.data.data);

        // Role-based smart redirection (strictly segregated)
        if (loggedUser.role === 'Merchant') {
          navigate('/portal/catalog', { replace: true });
        } else {
          // If previous location was inside /portal or was /login, always send staff to /
          if (!from || from === '/login' || from.startsWith('/portal')) {
            navigate('/', { replace: true });
          } else {
            navigate(from, { replace: true });
          }
        }
      } else {
        toast.error(res.data.message || 'بيانات الدخول غير صحيحة');
      }
    } catch (err: any) {
      toast.error(err.response?.data?.message || 'تعذر الاتصال بالخادم');
    } finally {
      setLoading(false);
    }
  };

  const setDemo = (email: string) => {
    setValue('email', email, { shouldValidate: true });
    setValue('password', 'Pass123456!', { shouldValidate: true });
  };

  return (
    <div className="min-h-screen bg-slate-100 dark:bg-slate-950 flex items-center justify-center p-4 select-none relative transition-colors duration-200" dir="rtl">
      {/* Top Bar with Theme Toggle */}
      <div className="absolute top-4 left-4 z-10">
        <button
          type="button"
          onClick={toggleTheme}
          title={theme === 'dark' ? 'التبديل إلى النمط الفاتح' : 'التبديل إلى النمط الداكن'}
          className="p-3 rounded-2xl bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-100 shadow-md border border-slate-200 dark:border-slate-700 hover:scale-105 transition-all flex items-center justify-center cursor-pointer"
        >
          {theme === 'dark' ? <Sun className="w-5 h-5 text-amber-400" /> : <Moon className="w-5 h-5 text-slate-700" />}
        </button>
      </div>
      <div className="max-w-md w-full bg-white dark:bg-slate-900 rounded-3xl shadow-2xl p-8 border border-slate-200/80 dark:border-slate-800 text-slate-900 dark:text-white transition-colors">
        <div className="text-center mb-8">
          <div className="inline-flex p-3 rounded-2xl bg-gradient-to-tr from-primary-600 to-indigo-600 text-white shadow-xl shadow-primary-500/30 mb-3">
            <Sparkles className="w-8 h-8" />
          </div>
          <h1 className="text-2xl font-black text-slate-900 dark:text-white">منظومة RetailOS</h1>
          <p className="text-slate-600 dark:text-slate-300 text-sm mt-1">
            إدارة مبيعات ومخازن المنظفات والأنشطة التجارية وبوابة الجملة
          </p>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <Input
            label="البريد الإلكتروني أو اسم المستخدم"
            type="text"
            placeholder="name@retailos.com أو بريد التاجر"
            icon={<Mail className="w-4 h-4" />}
            error={errors.email?.message}
            {...register('email')}
          />

          <Input
            label="كلمة المرور"
            type="password"
            placeholder="••••••••"
            icon={<Lock className="w-4 h-4" />}
            error={errors.password?.message}
            {...register('password')}
          />

          <Button
            type="submit"
            size="lg"
            isLoading={loading}
            className="w-full mt-2 shadow-lg shadow-primary-500/25"
          >
            تسجيل الدخول
          </Button>
        </form>

        <div className="mt-8 pt-6 border-t border-slate-200 dark:border-slate-800">
          <p className="text-xs text-slate-600 dark:text-slate-300 font-bold mb-3 flex items-center gap-1.5">
            <ShieldCheck className="w-4 h-4 text-primary-600" />
            حسابات تجريبية سريعة بضغطة واحدة:
          </p>
          <div className="grid grid-cols-3 gap-2">
            <button
              type="button"
              onClick={() => setDemo('owner@retailos.com')}
              className="px-2 py-2 bg-slate-100 dark:bg-slate-800 hover:bg-primary-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 hover:text-primary-700 dark:hover:text-primary-400 rounded-xl text-xs font-semibold transition border border-slate-200 dark:border-slate-700"
            >
              المالك (Owner)
            </button>
            <button
              type="button"
              onClick={() => setDemo('manager@retailos.com')}
              className="px-2 py-2 bg-slate-100 dark:bg-slate-800 hover:bg-primary-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 hover:text-primary-700 dark:hover:text-primary-400 rounded-xl text-xs font-semibold transition border border-slate-200 dark:border-slate-700"
            >
              المدير (Manager)
            </button>
            <button
              type="button"
              onClick={() => setDemo('cashier@retailos.com')}
              className="px-2 py-2 bg-slate-100 dark:bg-slate-800 hover:bg-primary-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 hover:text-primary-700 dark:hover:text-primary-400 rounded-xl text-xs font-semibold transition border border-slate-200 dark:border-slate-700"
            >
              الكاشير (Cashier)
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Login;
`;

fs.writeFileSync(targetPath, content, 'utf8');
console.log('Login.tsx updated with role-based redirection.');

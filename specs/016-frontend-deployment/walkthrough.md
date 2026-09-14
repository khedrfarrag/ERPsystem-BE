# Walkthrough: Frontend Cloud Deployment & Vercel Integration (016-frontend-deployment)

## Overview

The RetailOS React frontend (`system-FE`) is now fully prepared, optimized, and tested against the live remote production backend hosted on SmarterASP.NET (`https://khedrfarrag-001-site1.itempurl.com/api/v1`) and remote Neon PostgreSQL.

All 18 tasks across Setup, Foundational, Cloud Auth, Mandatory Password Change, and Production Optimization have been completed and verified.

---

## 🛠️ Summary of Changes Made

### 1. Environment & Vercel Routing Configuration
- **[NEW] `.env.production` & `.env.production.example`**: Configured canonical production API target:
  ```env
  VITE_API_URL=https://khedrfarrag-001-site1.itempurl.com/api/v1
  ```
- **[NEW] `vercel.json`**: Added client-side rewrite rules to prevent 404 errors on deep SPA route refreshes (`/pos`, `/inventory`, `/reports`):
  ```json
  {
    "rewrites": [
      { "source": "/(.*)", "destination": "/index.html" }
    ]
  }
  ```
- **[NEW] `public/_redirects` & `netlify.toml`**: Multi-platform static SPA fallback support.

### 2. Client-Side Authentication & Cloud API Handshake
- **`src/types/index.ts`**: Extended `User` interface to include `mustChangePassword?: boolean`.
- **`src/api/client.ts`**:
  - Enforced `Bearer {token}` injection on all requests.
  - Added user-friendly Arabic network error notifications (`ERR_NETWORK`).
  - Added `changePasswordApi` helper method.
- **`src/context/AuthContext.tsx`**: Added `clearMustChangePassword` to update local state and `localStorage` seamlessly upon password reset.
- **`src/pages/Login.tsx`**: Clean Arabic RTL typography, responsive error toast alerts, and one-click demo credentials for quick operational access.

### 3. Mandatory First-Login Password Change Flow
- **[NEW] `src/components/auth/ForceChangePasswordModal.tsx`**:
  - Non-dismissible modal overlay that blocks navigation when `user.mustChangePassword === true`.
  - Enforces password strength (minimum 8 chars, uppercase, lowercase, numeric).
  - Submits to backend endpoint `POST /api/v1/auth/change-password`.
- **`src/components/layout/Layout.tsx`**: Mounted `ForceChangePasswordModal` to automatically gate authenticated sessions.

### 4. Build Optimization & Bundle Chunking
- **`vite.config.ts`**: Rollup vendor chunk splitting:
  - `vendor`: React core and baseline libraries
  - `vendor-charts`: Recharts charting engine (loaded independently)
  - `vendor-query`: TanStack React Query & Axios
  - `vendor-icons`: Lucide React icons
- **Validation**: Full TypeScript typecheck (`tsc -b`) and production bundle build (`vite build`) completed with **0 errors**.

---

## 🚀 كيفية الرفع على Vercel خطوة بخطوة (How to Deploy to Vercel)

بما أنك اخترت **Vercel** (`https://vercel.com`)، هناك طريقتان سهلتان جداً للنشر:

### الطريقة الأولى: عبر لوحة تحكم Vercel (موصى بها ومجانية 100%)

1. **دخول Vercel**:
   - توجه إلى [vercel.com](https://vercel.com) وقم بتسجيل الدخول بحساب GitHub الخاص بك.
2. **إضافة المشروع الجديد**:
   - اضغط على زر **"Add New..."** ثم اختر **"Project"**.
3. **اختيار المستودع**:
   - ستجد قائمة بمستودعاتك على GitHub، اختر المستودع الخاص بالفرونت إند:
     `khedrfarrag/ERPsystemFE`
   - اضغط على **"Import"**.
4. **ضبط الإعدادات (Vercel سيكتشفها تلقائياً)**:
   - **Framework Preset**: سيظهر تلقائياً **Vite**.
   - **Root Directory**: اتركه `./`.
   - **Build Command**: `npm run build` (افتراضي).
   - **Output Directory**: `dist` (افتراضي).
5. **إضافة متغير البيئة (Environment Variables)**:
   - افتح قسم **Environment Variables** وأضف:
     - **Key**: `VITE_API_URL`
     - **Value**: `https://khedrfarrag-001-site1.itempurl.com/api/v1`
   - اضغط **Add**.
6. **الضغط على Deploy**:
   - اضغط **"Deploy"**!
   - خلال أقل من دقيقة واحدة، ستظهر شاشة الاحتفال وسيعطيك Vercel رابطاً حياً مجانياً وفورياً بصيغة:
     `https://erpsystem-fe.vercel.app` (أو اسم مشابه تختاره).

---

### الطريقة الثانية: الرفع المباشر عبر التيرمينال (Vercel CLI)

إذا أردت رفع المشروع مباشرة من جهازك الآن:
1. افتح التيرمينال وتوجه لمجلد الفرونت إند:
   ```bash
   cd g:\system-analysiss-saas\system-FE
   ```
2. شغل أمر النشر للإنتاج:
   ```bash
   npx vercel --prod
   ```
3. سيطلب منك تسجيل الدخول عبر المتصفح لمرة واحدة، ثم تأكيد اسم المشروع ونشره فوراً!

---

## 🔑 بيانات الدخول للإنتاج (Production Credentials)

- **البريد الإلكتروني**: `owner@retailos.com`
- **كلمة المرور المؤقتة**: `RetailOS@Prod2026!`
- **عند أول دخول**:
  - ستظهر نافذة تغيير كلمة المرور الإلزامية تلقائياً.
  - أدخل كلمة المرور المؤقتة أعلاه، ثم اختر كلمة مرور جديدة قوية (مثال: `RetailOS#Secure2026!`).
  - سيتم تأمين الحساب، وفتح لوحة التحكم ونقاط البيع والفواتير فوراً.

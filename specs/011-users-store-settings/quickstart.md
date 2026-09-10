# Quickstart & Verification Guide: Users Management & Store Settings

**Feature**: `011-users-store-settings`  
**Date**: 2026-09-09  
**Status**: Ready

---

## 1. Prerequisites

1. **Backend Running** on `http://localhost:5030`.
2. **Frontend Running** on `http://localhost:5173`.
3. **Login credentials**:
   - Owner: `owner@retailos.com` / `Pass123456!`
   - Manager: `manager@retailos.com` / `Pass123456!`
   - Cashier: `cashier@retailos.com` / `Pass123456!`

---

## 2. End-to-End Verification Scenarios

### Scenario 1: Browse Users & Register a New Cashier
1. Log in as `owner@retailos.com`.
2. Click "إعدادات المتجر والمستخدمين" in the sidebar or navigate to `/settings`.
3. On the "طاقم العمل والمستخدمين" tab, verify the existing staff table renders.
4. Click "إضافة مستخدم جديد".
5. Fill in:
   - First Name: "سارة"
   - Last Name: "أحمد"
   - Email: `sara.cashier@retailos.com`
   - Password: `Pass123456!`
   - Role: "كاشير (Cashier)"
6. Submit the form.
   - **Expected**: A success toast appears, the modal closes, and "سارة أحمد" appears in the users table with role badge "كاشير" and active status "نشط" (Green).

---

### Scenario 2: Toggle Staff Active / Inactive Status
1. Locate the newly created user "سارة أحمد" in the users table.
2. Click the status toggle switch on her row.
   - **Expected**: Visual toggle switches to off, status badge updates to "معطل" (Gray), and a success toast confirms status change.
3. Observe that for the currently logged-in Owner row, the toggle switch is disabled (cannot self-deactivate).

---

### Scenario 3: Update Store Profile & Verify Branding
1. Click the "بيانات وإعدادات المتجر" tab in `/settings`.
2. Update the Store Name to a new test name (e.g. "مؤسسة الأمل التجارية المتطورة").
3. Update Phone and Physical Address.
4. Toggle "السماح بالبيع بالسالب" (Allow Negative Stock) on or off.
5. Click "حفظ إعدادات المتجر".
   - **Expected**: Confirmation toast appears, and the top sidebar header branding immediately updates to the new store name without reloading the page.

---

### Scenario 4: Role-Based Authorization Access Check
1. Log out and log in as `cashier@retailos.com`.
2. Verify that the "إعدادات المتجر والمستخدمين" link is **not visible** in the sidebar.
3. Manually type `http://localhost:5173/settings` in the address bar.
   - **Expected**: The protected route intercepts and redirects the cashier to `/` or displays an unauthorized notice.

---

### Scenario 5: Build & Contrast Validation
1. Audit WCAG AA contrast (≥ 4.5:1 ratio) in Dark and Light modes.
2. Run build verification:
   ```bash
   cd g:\system-analysiss-saas\system-FE
   npm run build
   # Expected: 0 errors, build succeeds in <10s
   ```

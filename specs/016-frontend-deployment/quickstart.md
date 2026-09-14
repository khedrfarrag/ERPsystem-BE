# Quickstart & Validation Guide: Frontend Cloud Deployment & API Integration (016-frontend-deployment)

## Purpose
This guide provides actionable, end-to-end validation scenarios to verify that the React Single-Page Application (SPA) integrates with the live production backend API, enforces the mandatory first-login password change flow, and builds cleanly with optimized assets and deep SPA routing.

---

## 1. Prerequisites & Environment Setup

- **Node.js**: v20+ / npm v10+
- **Production Backend Endpoint**: `https://khedrfarrag-001-site1.itempurl.com/api/v1`
- **Initial Store Owner Credentials**:
  - Email: `owner@retailos.com`
  - Temporary Password: `RetailOS@Prod2026!`

---

## 2. Build & Local Preview Scenario

### Step 1: Verify Production Environment Variable
Ensure `g:/system-analysiss-saas/system-FE/.env.production` contains:
```env
VITE_API_URL=https://khedrfarrag-001-site1.itempurl.com/api/v1
```

### Step 2: Build Production Bundle
Run from `system-FE`:
```bash
npm run build
```
**Expected Outcome**:
- TypeScript compilation (`tsc -b`) succeeds with 0 errors.
- Vite generates hashed bundles inside `dist/`.
- Total compressed core JavaScript bundle remains under **350 KB**.

### Step 3: Run Production Preview Server
```bash
npm run preview -- --port 4173
```
**Expected Outcome**:
- Preview server listening on `http://localhost:4173`.
- Opening the preview loads the login page instantaneously (< 1.5s).

---

## 3. End-to-End Validation Scenarios

### Scenario 1: Authentication & Live Cloud API Handshake (User Story 1)
1. Navigate to `http://localhost:4173/login`.
2. Enter email: `owner@retailos.com`, password: `RetailOS@Prod2026!`.
3. Click "تسجيل الدخول".
4. **Expected Outcome**:
   - HTTP POST sent to `https://khedrfarrag-001-site1.itempurl.com/api/v1/auth/login`.
   - Returns 200 OK with `accessToken`, `refreshToken`, and user payload.
   - User token is saved in `localStorage`.
   - User is redirected into the authenticated layout.

---

### Scenario 2: Mandatory First-Login Password Change (User Story 2)
1. Upon landing on the dashboard after the initial login above, observe the screen.
2. **Expected Outcome**:
   - Because `mustChangePassword` is `true`, a persistent non-dismissible modal appears ("تحديث كلمة المرور الإلزامية").
   - Clicking outside the modal or pressing Escape does not dismiss the dialog.
3. In the modal form, enter:
   - Current Password: `RetailOS@Prod2026!`
   - New Password: `RetailOS#Secure2026!`
   - Confirm Password: `RetailOS#Secure2026!`
4. Click "حفظ وتأكيد كلمة المرور".
5. **Expected Outcome**:
   - HTTP POST sent to `/api/v1/auth/change-password`.
   - Receives 200 OK.
   - Green success toast displayed: "تم تحديث كلمة المرور بنجاح".
   - `mustChangePassword` flag updated to `false` in `localStorage` and `AuthContext`.
   - Modal closes automatically, unblocking full access to dashboard, POS, and navigation.

---

### Scenario 3: Deep Route Refresh & SPA Fallback (User Story 3)
1. Navigate to `http://localhost:4173/pos`.
2. Press `Ctrl + F5` (hard browser refresh) on `/pos`.
3. **Expected Outcome**:
   - Page does not display a 404 error.
   - Point of Sale layout rehydrates cleanly from cached authentication.
   - Products list and cart appear ready for checkout.
4. Test with `/inventory` and `/reports` routes similarly.

---

### Scenario 4: Live Inventory & Catalog Sync
1. In the POS screen, search for "محبس" or "ماسورة" (or scan a test barcode).
2. **Expected Outcome**:
   - Search dispatches debounced request to `/api/v1/products?search=...`.
   - Returns cloud database seeded plumbing products.
   - Products display with prices in EGP and stock counters.

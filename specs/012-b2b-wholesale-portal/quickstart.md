# Quickstart Validation Guide: B2B Wholesale Portal & Notifications

**Feature**: `012-b2b-wholesale-portal`  
**Date**: 2026-09-09  
**Status**: Ready for Validation

---

## 1. Prerequisites
- Running Backend API on `http://localhost:5030`.
- Running Frontend Dev Server on `http://localhost:5173`.
- Authenticated Store Owner credentials (`owner@retailos.com` / `Pass123456!`).

---

## 2. Validation Flow 1: Register Wholesale Merchant
1. Log into the system as `owner@retailos.com`.
2. Navigate to "إدارة تجار الجملة والمحلات" in the sidebar (`/merchants`).
3. Click "إضافة تاجر جديد":
   - اسم المحل / الاسم التجاري: "محل السلام ماركت"
   - اسم المسؤول: "إبراهيم حسن"
   - الهاتف: "01009876543"
   - البريد: "elsalam@merchant.com"
   - كلمة المرور: "Pass123456!"
   - سقف الائتمان: "40000"
4. Click "حفظ وإنشاء الحساب".
5. **Expected Outcome**:
   - Merchant appears in the table with Active status and credit limit 40,000 EGP.
   - Corresponding user account is created with role `Merchant`.

---

## 3. Validation Flow 2: Merchant Portal Ordering
1. Open an incognito / separate browser window and navigate to `http://localhost:5173/login`.
2. Log in with `elsalam@merchant.com` / `Pass123456!`.
3. Verify automatic redirection to `/portal` (B2B Wholesale Catalog).
4. Verify that administrative modules (POS, Dashboard, Expenses, Settings) are hidden.
5. Add items to order cart (e.g. 5 units of Product A).
6. Choose payment preference: "آجل" (Credit).
7. Submit the order.
8. **Expected Outcome**:
   - Order confirmation appears with order number `#B2B-1001`.
   - Order status shows `Pending`.

---

## 4. Validation Flow 3: Notifications & Invoicing
1. Return to the Owner window (`http://localhost:5173`).
2. Notice the notification bell badge in the header increments by `1`.
3. Click the notification bell to view: "طلب توريد جديد #B2B-1001 من محل السلام ماركت".
4. Navigate to "طلبات التوريد والجملة" (`/b2b-orders`).
5. Open order `#B2B-1001` and click "اعتماد وتحويل لفاتورة مبيعات".
6. Enter cash down-payment (e.g. 1,000 EGP) and approve remaining on credit.
7. Confirm conversion.
8. **Expected Outcome**:
   - Order status changes to `Invoiced`.
   - Product inventory is automatically decremented.
   - Merchant ledger in customers section records the invoice debit and cash credit.

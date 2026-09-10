# Quickstart & Verification Guide: Operations Hardening

**Feature**: `013-operations-fixes-hardening`  
**Date**: 2026-09-10  

---

## Prerequisites
1. Backend running: `dotnet run --project src/RetailOS.Api` on port `5030`.
2. Frontend running: `npm run dev` on port `5173`.
3. User logged in as `owner@retailos.com` / `Pass123456!`.

---

## Test Scenario 1: WhatsApp Message Accuracy in B2B Modal
1. Go to `http://localhost:5173/b2b/orders`.
2. Click "عرض ومراجعة الطلب" for an Approved order.
3. Click the WhatsApp button in the modal header.
4. Verify the opened link contains:
   `https://wa.me/201...?text=...%20تمت%20المراجعة%20والاعتماد...`
   and NOT `مرفوض`.

---

## Test Scenario 2: Price Below Cost Rejection
1. In the same order approval modal, attempt to edit wholesale price to `0` or below purchase cost.
2. Verify:
   - UI shows a red error badge: "السعر أقل من التكلفة".
   - Submit button is disabled or backend rejects with `PRICE_BELOW_COST`.

---

## Test Scenario 3: Cash Drawer Shift Closing & Zeroing
1. Go to Expenses / Cash Register (`/expenses`).
2. Verify current drawer balance is positive (e.g., 500 EGP).
3. Click "إغلاق الوردية وتسوية الدرج".
4. Enter counted amount (e.g., 500 EGP), click confirm.
5. Verify drawer balance becomes exactly `0.00 EGP` and status indicates closed.

---

## Test Scenario 4: Store Settings Auto-Save & Tax Calculation
1. Go to Settings -> Store (`/settings`).
2. Toggle "تفعيل ضريبة القيمة المضافة (14% VAT)".
3. Verify toast notification appears confirming update. Refresh page; confirm toggle remains ON.
4. Go to POS, add items worth 100 EGP; confirm tax is 14 EGP and total is 114 EGP.

---

## Test Scenario 5: Staff Users Isolation & Merchant Synchronization
1. Go to Settings -> Users (`/settings`).
2. Confirm no merchant accounts appear in the list.
3. Go to Merchants (`/b2b/merchants`), toggle a merchant to inactive.
4. Attempt to log in with that merchant's credentials; confirm login is blocked.

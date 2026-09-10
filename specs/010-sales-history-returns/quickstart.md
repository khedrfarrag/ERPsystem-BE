# Quickstart & Verification Guide: Sales Invoices History & Returns

**Feature**: `010-sales-history-returns`  
**Date**: 2026-09-06  
**Status**: Ready

---

## 1. Prerequisites

1. **Backend Service Running**:
   ```bash
   cd g:\system-analysiss-saas\system-BE
   dotnet run --project src/RetailOS.Api
   # Accessible at http://localhost:5030
   ```
2. **Frontend Dev Server Running**:
   ```bash
   cd g:\system-analysiss-saas\system-FE
   npm run dev
   # Accessible at http://localhost:5173
   ```
3. **Seeded Demo Data**:
   Ensure demo store data is seeded via `POST http://localhost:5030/api/seed/demo-store?force=true` or login as:
   - Owner: `owner@retailos.com` / `Pass123456!`
   - Manager: `manager@retailos.com` / `Pass123456!`

---

## 2. End-to-End Validation Scenarios

### Scenario 1: Browse Invoices & Filter by Payment Method
1. Log in to the application and navigate to `/sales` from the sidebar ("سجل الفواتير والمرتجعات").
2. Verify that existing sales invoices are listed in chronological order (newest first).
3. Click the "نقدي" (Cash) filter tab.
   - **Expected**: Only invoices with payment method `Cash` remain visible.
4. Type an invoice number (e.g. `INV-`) into the search bar.
   - **Expected**: The table filters immediately to matching invoices.

---

### Scenario 2: View Invoice Breakdown & Reprint 80mm Receipt
1. Click on any invoice row or the "عرض التفاصيل" (View Details) button.
2. Verify the modal displays:
   - Header with invoice number, date, and customer name.
   - Itemized product table with quantities, prices, discounts, and line subtotals.
   - Financial breakdown: Subtotal, Tax, Total, Paid Cash, Credit Debt.
3. Click the "طباعة الإيصال" (Print Receipt) button.
   - **Expected**: Browser print dialog opens with a cleanly formatted 80mm thermal receipt, including barcode and store information. Cancel the dialog to return to the application.

---

### Scenario 3: Process a Partial Return with Cash Refund
1. With the invoice modal open, click "إجراء مرتجع" (Process Return).
2. The Return Wizard modal appears.
3. For one of the products with quantity ≥ 2, set the return quantity to 1.
4. Select the return reason (e.g. "منتج تالف / معيب").
5. Choose refund destination: "نقداً من الخزينة" (Cash).
6. Click "تأكيد واسترجاع المبلغ" (Confirm Return).
   - **Expected Outcome**:
     - Confirmation notification appears showing the return voucher number `SR-YYYYMMDD-...`.
     - Invoice status updates to "مرتجع جزئي" (amber badge).
     - Product stock increases by 1 in `/products`.
     - Cash register balance in `/expenses` decreases by the refunded amount.

---

### Scenario 4: UI/UX Contrast & Dark Mode Audit
1. Toggle the Dark Mode switch in the top header.
2. Verify that:
   - Table headers maintain high contrast (`bg-slate-900 text-white`).
   - Table row hover states are clearly visible (`dark:hover:bg-slate-700/60`).
   - Status badges remain easily readable (Contrast ratio ≥ 4.5:1).
   - Return modal inputs and buttons are well-defined.
3. Run the automated build check:
   ```bash
   cd g:\system-analysiss-saas\system-FE
   npm run build
   # Expected: 0 errors, build succeeds in <40s
   ```

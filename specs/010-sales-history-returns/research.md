# Research & Technical Decisions: Sales Invoices History & Returns Management

**Feature**: `010-sales-history-returns`  
**Date**: 2026-09-06  
**Status**: Completed

---

## 1. Context & Objectives

The goal of this feature is to deliver an end-to-end sales invoices history and returns management module on the frontend, integrating seamlessly with existing backend endpoints (`/api/sales`, `/api/sales/{id}`, `/api/sales/{id}/returns`, `/api/cash-register/current`).

Key capabilities required:
1. Searchable, filterable, server-paginated invoices table.
2. Comprehensive invoice detail drawer/modal with itemized breakdown and financial summary.
3. 80mm thermal receipt reprint preview and browser print integration.
4. Interactive return wizard allowing line-item quantity selection, return reason input, and refund routing (`Cash` vs. `Credit`).
5. Real-time feedback and state update without full page reload.
6. Full adherence to WCAG AA contrast standards in both Dark and Light modes.

---

## 2. Technical Decisions & Tradeoffs

### Decision 1: Frontend State Management for Sales & Returns
- **Decision**: Use React local component state with dedicated custom hook `useSales` and clean Axios service layer in `src/features/sales/api/salesApi.ts`.
- **Rationale**: Keeps state close to the consumer, follows existing frontend patterns established in `src/features/products`, `src/features/customers`, and `src/features/expenses`. Avoids unnecessary global store complexity while ensuring clean separation of concerns.
- **Alternatives Considered**: Redux Toolkit / Zustand — rejected as premature abstraction (violates Constitution Principle II).

### Decision 2: Thermal Receipt Printing Strategy
- **Decision**: Leverage browser native printing (`window.print()`) via a hidden `@media print` thermal receipt component with CSS dimensions fixed at 80mm (width: 72mm-80mm printable area, font monospace/Inter, barcode rendered as clean SVG or styled text, 0 margins).
- **Rationale**: Requires 0 heavy third-party printing libraries, works offline and across all browsers (Chrome/Edge/Firefox), and natively supports both physical thermal POS printers (e.g. Epson, Xprinter, Rongta) and "Save as PDF".
- **Alternatives Considered**: 
  - Third-party PDF generation (e.g., `jsPDF`) — rejected due to heavy bundle overhead (>300KB) and blurry text on thermal paper.
  - Direct ESC/POS WebUSB/WebBluetooth — rejected as it requires browser-specific experimental flags and driver bypass.

### Decision 3: Return Validation & Inventory/Drawer Safety Guards
- **Decision**: Perform client-side boundary checks prior to dispatching `POST /api/sales/{id}/returns`:
  1. Calculate `maxReturnableQty = item.quantity - previouslyReturnedQty`. If `maxReturnableQty <= 0`, disable selection.
  2. If `refundMethod === 'Cash'`, verify that the active cash drawer balance is sufficient (`drawer.currentBalance >= totalRefundAmount`). If insufficient or drawer is closed, display an actionable warning modal.
  3. If `refundMethod === 'Credit'`, verify that the invoice has a linked `customerId`. If anonymous walk-in sale, disable `Credit` refund option.
- **Rationale**: Prevents avoidable 400/409 errors from reaching the server, provides instant user feedback, and enforces business integrity (Constitution Principle VI).

### Decision 4: UI/UX & WCAG AA Contrast Standardization
- **Decision**: Use standardized `.table-header` (`bg-slate-100 text-slate-700 dark:bg-slate-900 dark:text-white`), `.table-row-hover` (`hover:bg-slate-50 dark:hover:bg-slate-700/60`), and high-contrast status badge tokens:
  - `Completed`: `bg-emerald-100 text-emerald-800 dark:bg-emerald-950/80 dark:text-emerald-300`
  - `Partially Returned`: `bg-amber-100 text-amber-800 dark:bg-amber-950/80 dark:text-amber-300`
  - `Fully Returned`: `bg-slate-100 text-slate-800 dark:bg-slate-800 dark:text-slate-300`
- **Rationale**: Adheres to Constitution UI Polish rules and solves text-contrast clashing in dark/light mode.

---

## 3. Unknowns & Resolved Questions

| Question / Unknown | Resolution |
| :--- | :--- |
| **Q1**: Can Cashiers process returns, or is it restricted to Owners and Managers? | The backend `POST /api/sales/{id}/returns` has `[Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]`. In the UI, the "إجراء مرتجع" button will check user role from `useAuthStore` and only display for Owner/Manager, or prompt with an access restriction notice if a cashier attempts it. |
| **Q2**: How should partial returns affect the invoice status? | If some items are returned, badge shows "مرتجع جزئي" (amber). If all quantities of all items are returned, badge shows "مرتجع كلي" (slate). |
| **Q3**: Does the backend support filtering by date range and search term? | Backend `GET /api/sales` accepts `customerId`, `paymentMethod`, `pageNumber`, and `pageSize`. Frontend client will provide instant client-side invoice number search across loaded pages or paginate via the backend. |

---

## 4. Best Practices Applied

1. **Idempotency & Double Submit Prevention**: Return submit button is disabled immediately upon click with a loading spinner.
2. **Deterministic Rounding**: All monetary calculations use 2 decimal places with standard commercial rounding.
3. **Accessibility**: All interactive elements have descriptive aria labels and WCAG AA contrast (≥ 4.5:1).

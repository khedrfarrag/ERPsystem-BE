# Tasks: Sales Invoices History & Returns Management

**Feature**: `010-sales-history-returns`  
**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)  
**Status**: Completed

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Module initialization and basic folder structure

- [X] T001 Create sales feature directory structure in `src/features/sales/api`, `src/features/sales/types`, `src/features/sales/hooks`, and `src/features/sales/components`
- [X] T002 [P] Define TypeScript interfaces and DTOs in `src/features/sales/types/sales.types.ts`
- [X] T003 [P] Implement Axios API client functions for sales and returns in `src/features/sales/api/salesApi.ts`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core routing, navigation, and state hook required by all user stories

- [X] T004 Register `/sales` route with ProtectedRoute in `src/App.tsx`
- [X] T005 Add Sales Invoices & Returns navigation item with icon in `src/components/layout/Sidebar.tsx`
- [X] T006 Implement data fetching, filtering, pagination, and return dispatching in `src/features/sales/hooks/useSales.ts`

---

## Phase 3: User Story 1 - Browse, Search, and Filter Past Invoices (Priority: P1) 🎯 MVP

**Goal**: Cashiers and managers can view, search, and filter the list of all past sales invoices

**Independent Test**: Navigate to `/sales`, filter by payment method or search by invoice number/customer, and verify invoices render accurately with correct totals and badges.

- [X] T007 [P] [US1] Implement search and filter controls in `src/features/sales/components/SalesFilters.tsx`
- [X] T008 [P] [US1] Implement high-contrast invoices table with pagination in `src/features/sales/components/SalesTable.tsx`
- [X] T009 [US1] Implement main Sales History page with summary KPI cards in `src/pages/Sales.tsx`

**Checkpoint**: User Story 1 is fully functional and testable independently as an MVP.

---

## Phase 4: User Story 2 - View Invoice Details & Thermal Receipt Reprint (Priority: P1)

**Goal**: Inspect complete itemized invoice breakdown and reprint 80mm thermal receipts

**Independent Test**: Open any invoice row, inspect products, unit prices, discounts, and tax, and click "Print Receipt" to verify 80mm printable layout.

- [X] T010 [P] [US2] Implement 80mm thermal receipt printable layout with `@media print` styles in `src/features/sales/components/ThermalReceiptPrint.tsx`
- [X] T011 [US2] Implement comprehensive invoice details modal with reprint trigger in `src/features/sales/components/SaleDetailsModal.tsx`

**Checkpoint**: User Stories 1 and 2 are fully functional and integrated.

---

## Phase 5: User Story 3 - Full and Partial Sales Returns with Automated Refund Routing (Priority: P1)

**Goal**: Process partial or full item returns, restore stock, and refund via Cash Drawer or Customer Credit

**Independent Test**: Select an invoice, return 1 unit of an item, select reason and Cash refund, and verify return reference number, drawer balance update, and stock restoration.

- [X] T012 [US3] Implement Return Wizard modal with returnable quantity steppers and reason selection in `src/features/sales/components/SaleReturnModal.tsx`
- [X] T013 [US3] Implement validation guards for cash drawer balance and customer credit eligibility in `src/features/sales/components/SaleReturnModal.tsx`
- [X] T014 [US3] Connect return submission to `createSaleReturn` API and update invoice state without full page reload in `src/pages/Sales.tsx` and `src/features/sales/components/SaleDetailsModal.tsx`

**Checkpoint**: User Stories 1, 2, and 3 are fully functional.

---

## Phase 6: User Story 4 - Audit Return History and Reversal Tracking (Priority: P2)

**Goal**: Maintain clear audit trail of all prior return vouchers and display dynamic return status badges

**Independent Test**: Open an invoice with previous returns, verify return vouchers list with timestamps and reasons, and confirm table badge shows "مرتجع جزئي" or "مرتجع كلي".

- [X] T015 [P] [US4] Add past return vouchers list and audit details to `src/features/sales/components/SaleDetailsModal.tsx`
- [X] T016 [US4] Implement dynamic return status badges (`Completed`, `PartiallyReturned`, `FullyReturned`) in `src/features/sales/components/SalesTable.tsx`

---

## Phase 7: Polish & Verification

**Purpose**: Design consistency, accessibility, and production build verification

- [X] T017 [P] Verify WCAG AA contrast (≥ 4.5:1) for table headers, rows, badges, and modals in Dark and Light modes using Chrome DevTools
- [X] T018 Run frontend build validation (`npm run build` in `system-FE`) to ensure 0 TypeScript or bundling errors

---

## Dependencies & Execution Order

### Phase Dependencies
- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 — blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Phase 2 — delivers MVP.
- **User Story 2 (Phase 4)**: Depends on Phase 3.
- **User Story 3 (Phase 5)**: Depends on Phase 4 (opens from invoice details).
- **User Story 4 (Phase 6)**: Depends on Phase 5.
- **Polish (Phase 7)**: Depends on all user stories.

### Parallel Opportunities
- `T002` and `T003` can run in parallel in Phase 1.
- `T007` and `T008` can run in parallel in Phase 3.
- `T010` and `T015` can be developed in parallel with their respective parent modals.
- `T017` and `T018` can run in parallel during the polish phase.

---

## Implementation Strategy

### MVP First (User Story 1)
1. Complete Phase 1 (Setup) + Phase 2 (Foundational).
2. Complete Phase 3 (User Story 1 - Sales Invoices Browsing & Filtering).
3. **Validate**: Cashiers can browse and search past invoices immediately.

### Incremental Delivery
1. Add User Story 2: Thermal receipt reprinting and detailed inspection.
2. Add User Story 3: Returns wizard with stock and cash drawer adjustments.
3. Add User Story 4: Return voucher auditing and status indicators.
4. Polish & Build verification.

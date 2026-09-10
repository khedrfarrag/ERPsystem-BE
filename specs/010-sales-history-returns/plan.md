# Implementation Plan: Sales Invoices History & Returns Management

**Branch**: `010-sales-history-returns` | **Date**: 2026-09-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/010-sales-history-returns/spec.md`

---

## Summary

This feature delivers the Sales Invoices History and Returns Management interface in RetailOS. It allows cashiers and store managers to search, filter, and inspect past sales transactions, reprint thermal receipts matching POS standard 80mm dimensions, and process full or partial item returns with automated stock restoration and intelligent refund routing (`Cash` from the active cash drawer or `Credit` applied to customer account receivables).

---

## Technical Context

**Language/Version**: TypeScript 5.2 / React 18 (Frontend), C# 12 / .NET 8.0 LTS (Backend)  
**Primary Dependencies**: React Router v6, Axios, Lucide React, Tailwind CSS, date-fns  
**Storage**: PostgreSQL (existing tables: `sales`, `sale_line_items`, `sale_returns`, `sale_return_items`, `cash_register_transactions`, `customer_account_transactions`)  
**Testing**: Chrome DevTools automated DOM inspection, Vite/TypeScript compilation (`npm run build`), API endpoint validation via Swagger/Axios  
**Target Platform**: Modern Web Browsers (Desktop & Tablet POS displays), Thermal receipt printers (80mm standard)  
**Project Type**: Full-Stack SaaS Web Application (Modular Monolith backend + React SPA frontend)  
**Performance Goals**: Sub-300ms client search filtering, <500ms API response for invoice retrieval and return processing, <1s print receipt dialog launch  
**Constraints**: Zero full-page reloads, strict WCAG AA contrast compliance (≥ 4.5:1 ratio), cash refund blocked if drawer closed or insufficient funds  
**Scale/Scope**: Supports hundreds of daily sales per register, paginated server-side (20-100 items per page)  

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

- [X] **Simplicity First (KISS)**: Follows existing feature module conventions (`api/`, `components/`, `types/`, `hooks/`) without adding third-party printing bloat or unnecessary state libraries.
- [X] **No Premature Abstraction (YAGNI)**: Uses native `window.print()` and standard Axios REST requests directly to `/api/sales` and `/api/sales/{id}/returns`.
- [X] **DRY Without Over-Engineering**: Reuses table header/hover styles (`.table-header`, `.table-row-hover`), modal wrappers, and currency formatting utilities.
- [X] **Business Integrity**: Returns are atomic backend operations that cannot exceed purchased quantities; returns restore inventory via `inventory_transactions` and update ledger tables immutably.
- [X] **Tenant Isolation**: All requests inherit active store context from the JWT session; no cross-store data leakage is possible.
- [X] **Role-Based Authorization**: Sales history viewable by Cashier/Manager/Owner; Return submission restricted to Owner/Manager per backend policy.

---

## Project Structure

### Documentation (this feature)

```text
specs/010-sales-history-returns/
├── spec.md                  # Feature Specification
├── plan.md                  # This Implementation Plan
├── research.md              # Technical research & decisions
├── data-model.md            # Data models, entities & state transitions
├── quickstart.md            # End-to-end verification guide
├── contracts/               # API & Component contracts
│   └── sales-history-contracts.md
├── checklists/              # Quality checklist
│   └── requirements.md
└── tasks.md                 # Implementation tasks (generated in next phase)
```

### Source Code Layout

```text
# Frontend Source (g:/system-analysiss-saas/system-FE)
src/
├── features/
│   └── sales/
│       ├── api/
│       │   └── salesApi.ts               # Axios API client for sales & returns
│       ├── types/
│       │   └── sales.types.ts            # DTOs & state interfaces
│       ├── hooks/
│       │   └── useSales.ts               # State, filter, & pagination hook
│       └── components/
│           ├── SalesFilters.tsx          # Search, customer & payment filters
│           ├── SalesTable.tsx            # High-contrast table with status badges
│           ├── SaleDetailsModal.tsx      # Breakdown modal & reprint trigger
│           ├── SaleReturnModal.tsx       # Return wizard with quantity validation
│           └── ThermalReceiptPrint.tsx   # 80mm printable thermal receipt
├── pages/
│   └── Sales.tsx                         # Main Sales Invoices History page
├── components/layout/
│   └── Sidebar.tsx                       # Added navigation link to /sales
└── App.tsx                               # Route registration for /sales
```

**Structure Decision**: Adopts the established feature-sliced folder structure in `src/features/sales` matching all existing modules (`products`, `customers`, `suppliers`, `expenses`, `reports`), ensuring 100% architectural consistency.

---

## Complexity Tracking

*No constitutional violations or unnecessary complexity detected. All requirements fit within standard patterns.*

# Implementation Plan: Operations Hardening & Business Workflow Fixes

**Branch**: `013-operations-fixes-hardening` | **Date**: 2026-09-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/013-operations-fixes-hardening/spec.md`

---

## Summary

This plan hardens core business workflows across 5 critical modules:
1. **B2B Wholesale Module**: Fixes WhatsApp status sync in `B2BOrderDetailsModal.tsx` and establishes a non-negotiable below-cost pricing guard in both frontend and backend (`B2BOrderService.cs`).
2. **Cash Register & Shifts**: Enhances shift close in `CashRegisterService.cs` to zero out the drawer via an audited cash drawer sweep (`CashDrop`) to the safe, eliminating cumulative multi-shift balances.
3. **Store Policies & VAT**: Implements instant toggle persistence in `StoreProfileTab.tsx` and applies 14% VAT calculation in `SaleService.cs` and POS when enabled.
4. **Supplier Statement Drill-Down**: Provides complete purchase invoice line-item visibility directly from the supplier statement modal.
5. **Staff Isolation & Merchant Sync**: Isolates internal staff management in `UserService.cs` and ensures synchronous login blocking when merchants are deactivated in `MerchantService.cs`.

---

## Technical Context

- **Backend**: .NET 8 / ASP.NET Core Web API, EF Core 9 with Npgsql (PostgreSQL), FluentValidation.
- **Frontend**: React 18, TypeScript, Tailwind CSS, TanStack Query, React Hook Form, Lucide React.
- **Architecture Constraints**: Multi-tenant isolation by `StoreId`, append-only transaction ledger, decimal precision using `MidpointRounding.AwayFromZero`.

---

## Constitution Check

| Principle | Evaluation | Status |
| :--- | :--- | :--- |
| **Correctness** | Financial transactions, VAT calculations, and cash sweep preserve exact ledger balances with zero loss. | PASS |
| **Security & Auth** | Merchant deactivation immediately updates `User.IsActive`, blocking unauthorized access. | PASS |
| **Tenant Isolation** | All queries remain strictly partitioned by `StoreId`. | PASS |
| **Ledger Immutability** | Shift closing records adjustments and sweep transactions rather than mutating past records. | PASS |

---

## Implementation Phases

### Phase 1: B2B Pricing Safeguard & WhatsApp Correction
- [x] Pass `status`, `totalAmount`, `paymentPreference` to `buildOrderWhatsAppUrl` in `B2BOrderDetailsModal.tsx`.
- [x] Add fallback default in `whatsappUtils.ts` to "قيد المراجعة" rather than "مرفوض".
- [x] Add guard in `B2BOrderService.ApproveOrderAsync` preventing wholesale price < purchase cost.
- [x] Add client-side validation badge and min-value constraint in `B2BOrderDetailsModal.tsx`.

### Phase 2: Cash Drawer Shift Closing & Sweep
- [x] Update `CashRegisterService.CloseRegisterAsync` to record a `CashDrop` sweep for `-request.CountedAmount`.
- [x] Update `CashRegisterCloseResponse` with `SweepAmount`.
- [x] Update `CloseRegisterModal.tsx` to reflect drawer zeroing and auto-refresh cash summaries.

### Phase 3: Store Settings Auto-Save & 14% VAT
- [x] Wire instant toggle handler in `StoreProfileTab.tsx` for `taxEnabled` and `allowNegativeStock`.
- [x] Integrate 14% VAT calculation in `SaleService.cs` when `store.TaxEnabled` is true.
- [x] Update POS payment modal to display VAT 14% breakdown when active.

### Phase 4: Supplier Statement Purchase Breakdown
- [x] In `SupplierStatementModal.tsx`, add an expandable drill-down button on purchase rows fetching `/api/purchases/{referenceId}` to render items, quantities, and costs.

### Phase 5: Staff Users Isolation & Merchant Synchronization
- [x] In `UserService.GetUsersAsync`, filter `query.Where(u => u.Role != Roles.Merchant)`.
- [x] In `MerchantService.ToggleActiveAsync`, update the linked `User.IsActive` simultaneously.

---

## Verification & Validation
- Automated compilation check (`dotnet build`).
- End-to-end testing of each scenario per [quickstart.md](./quickstart.md).

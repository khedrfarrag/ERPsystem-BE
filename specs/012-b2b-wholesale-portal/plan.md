# Implementation Plan: B2B Wholesale Portal & Multi-channel Notifications

**Branch**: `012-b2b-wholesale-portal` | **Date**: 2026-09-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/012-b2b-wholesale-portal/spec.md`

---

## Summary

Implement a full-cycle B2B Wholesale & Supply Portal module for RetailOS:
1. **Backend Core**:
   - Entities: `Merchant`, `B2BOrder`, `B2BOrderItem`, `Notification`.
   - Role authorization: `Merchant` added to ASP.NET Core Identity.
   - EF Core Global Query Filter on `StoreId` for tenant isolation.
   - Services: `IB2BOrderService`, `IMerchantService`, `INotificationService`, `IEmailNotificationService`, `IWhatsAppNotificationService`.
   - Invoicing pipeline: Atomic transaction turning approved orders into `Sale` records, decrementing stock via `CentralizedInventoryService`, and debiting customer accounts ledger.
2. **Frontend Applications**:
   - Store Owner / Admin:
     - `Merchants` management page (`/merchants`).
     - `B2B Orders` management and review page (`/b2b-orders`).
     - In-App Notification Center in Header (`NotificationDropdown.tsx`).
    - Merchant Wholesale Portal:
      - Portal layout and B2B catalog view (`/portal`).
      - Order assembly and checkout cart with credit limit checks.
      - My Orders history and Statement of Account (`/portal/orders`, `/portal/statement`).
3. **Operational Refinement & Remediation (Session 2026-09-10)**:
   - Stabilize Statement of Account (`AccountStatementResponse`) mapping to prevent frontend `TypeError`.
   - Protect catalog and cart from zero-stock additions; provide automated deficit zeroing for Owner.
   - Expose remaining stock and confidential purchase costs to Owner; enable line price and invoice discount adjustments.
   - Surface rejection reason immediately on merchant order cards.
   - Capture expected down-payment for partial payment orders.
   - Compute and display cash change due ("الفكة") when cash received exceeds invoice total.

---

## Technical Context

**Language/Version**: C# .NET 8 (ASP.NET Core Web API) / TypeScript 5.7+ (React 19 + Vite)  
**Primary Dependencies**: EF Core, Npgsql, ASP.NET Core Identity, JWT, React Router 7, TanStack Query, Lucide React, TailwindCSS  
**Storage**: PostgreSQL 16 (`merchants`, `b2b_orders`, `b2b_order_items`, `notifications`, `sales`, `customer_account_transactions`)  
**Testing**: xUnit, FluentAssertions, Manual/Browser end-to-end scenarios  
**Target Platform**: Linux/Windows Web Server + Modern Browsers (Chrome, Edge, Firefox, Safari)  
**Project Type**: Modular Monolith Web Application (Backend API + Single Page Application)  
**Performance Goals**: Order placement < 500ms; Header unread notification poll < 100ms; Conversion to invoice < 1s  
**Constraints**: Absolute tenant isolation; High-contrast WCAG AA (≥ 4.5:1 ratio); Strict ledger audit trails; No hard deletes of financial records  
**Scale/Scope**: ~15 new API endpoints, 4 new database entities, 2 new admin pages, 1 merchant portal interface, 1 in-app notification center  

---

## Constitution Check

*GATE: Must pass before implementation. Re-checked post-design.*

- [x] **Absolute Constraints**: Correctness (atomic transactions for invoice conversion and decimal precision), Security (role-based authorization `Merchant`), Tenant Isolation (`StoreId` global query filter on all 4 new entities).
- [x] **Principle I (KISS)**: Simple, direct endpoints and modular services without unnecessary abstractions.
- [x] **Principle II (YAGNI)**: No message brokers, microservices, or complex event buses; notifications stored in PostgreSQL table with polling.
- [x] **Principle III (DRY)**: Reuses existing `Customer` entity and `customer_account_transactions` for financial ledger balance tracking.
- [x] **Principle V (Strong Typing)**: Explicit DTOs for all requests and responses; magic numbers and roles eliminated via enums.
- [x] **Principle VI (Business Integrity)**: Invoicing is atomic via DB transaction; physical stock is decremented exclusively via `CentralizedInventoryService`.

---

## Project Structure

### Documentation (this feature)

```text
specs/012-b2b-wholesale-portal/
├── spec.md              # Feature specification
├── plan.md              # This implementation plan
├── research.md          # Architectural research and decisions
├── data-model.md        # Entity definitions and state machines
├── quickstart.md        # Step-by-step verification guide
├── contracts/
│   └── b2b-contracts.md # API endpoint specifications
└── tasks.md             # Implementation tasks (to be created by /speckit-tasks)
```

### Source Code Layout

#### Backend (`system-BE/src`)
```text
RetailOS.Domain/
├── Entities/
│   ├── Merchant.cs
│   ├── B2BOrder.cs
│   ├── B2BOrderItem.cs
│   └── Notification.cs
RetailOS.Infrastructure/
├── Persistence/
│   ├── Configurations/ (Entity configurations & indices)
│   └── Migrations/ (EF Core migration: AddB2BWholesaleAndNotifications)
├── Services/
│   ├── EmailNotificationService.cs
│   └── WhatsAppNotificationService.cs
RetailOS.Application/
├── B2B/ (DTOs, Services, Interfaces)
│   ├── IMerchantService.cs
│   ├── IB2BOrderService.cs
│   └── INotificationService.cs
RetailOS.Api/
├── Controllers/
│   ├── MerchantsController.cs
│   ├── B2BOrdersController.cs
│   └── NotificationsController.cs
```

#### Frontend (`system-FE/src`)
```text
features/b2b/
├── api/ (merchantsApi.ts, b2bOrdersApi.ts, notificationsApi.ts)
├── types/ (b2b.types.ts)
├── hooks/ (useMerchants.ts, useB2BOrders.ts, useNotifications.ts)
└── components/
    ├── MerchantsTable.cs
    ├── CreateMerchantModal.tsx
    ├── B2BOrdersTable.tsx
    ├── B2BOrderDetailsModal.tsx
    └── NotificationDropdown.tsx
pages/
├── Merchants.tsx (Admin view)
├── B2BOrders.tsx (Admin view)
└── portal/
    ├── PortalCatalog.tsx (Merchant view)
    ├── PortalCartModal.tsx
    ├── PortalOrders.tsx
    └── PortalStatement.tsx
```

---

## Implementation Phases

1. **Phase 1: Domain Entities & Database Migrations**:
   - Define `Merchant`, `B2BOrder`, `B2BOrderItem`, and `Notification` entities.
   - Configure EF Core mappings, foreign keys, and multi-tenant query filters.
   - Run `dotnet ef migrations add AddB2BWholesaleAndNotifications`.
2. **Phase 2: Core Application Services & Notification Dispatch**:
   - Implement `MerchantService`, `B2BOrderService`, and `NotificationService`.
   - Implement `EmailNotificationService` and `WhatsAppNotificationService`.
   - Implement atomic sales invoice conversion connecting to `CentralizedInventoryService` and customer ledger.
3. **Phase 3: API Controllers & Security**:
   - Implement `MerchantsController`, `B2BOrdersController`, and `NotificationsController`.
   - Add role authorization attributes (`[Authorize(Roles = "Owner,Manager")]`, `[Authorize(Roles = "Merchant")]`).
4. **Phase 4: Admin Frontend (Merchants, B2B Orders, Header Notifications)**:
   - Header notification bell with live polling and dropdown.
   - Admin Merchants management screen (`/merchants`).
   - Admin B2B Orders screen with review modal and invoice conversion (`/b2b-orders`).
5. **Phase 5: Merchant Portal (Self-Service Catalog & Statements)**:
   - Dedicated portal layout for role `Merchant` (`/portal`).
   - B2B product catalog with wholesale pricing and checkout cart.
   - Order history and account statement.
6. **Phase 6: Polish & Initial Verification**:
   - WCAG AA contrast check.
   - Full production build validation (`npm run build`).
7. **Phase 7: Operational Polish & Remediation**:
   - Align Statement of Account DTO (`partyName`, `date`, `runningBalance`) across API and frontend.
   - Restrict catalog and cart from zero-stock additions; implement auto-zeroing assistance for Owner review.
   - Expose remaining warehouse stock and purchase cost (restricted to Owner/Manager); support invoice discount and price adjustment.
   - Make rejection reason immediately visible on merchant order cards.
   - Add expected down-payment input for `Partial` payment orders.
   - Implement dynamic change due calculation ("الفكة") when cash received exceeds invoice total.

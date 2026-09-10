# Tasks: B2B Wholesale Orders, Merchant Portal & Multi-channel Notifications

**Feature**: `012-b2b-wholesale-portal`  
**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)  
**Status**: Ready for Implementation

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Module initialization, folder structure, and shared types across backend and frontend

- [X] T001 Create feature directory structure in `src/RetailOS.Application/B2B`, `src/features/b2b/{api,types,hooks,components,utils}`, and `src/pages/portal`
- [X] T002 [P] Define TypeScript interfaces and DTOs in `src/features/b2b/types/b2b.types.ts`
- [X] T003 [P] Define C# DTOs for wholesale merchants, B2B orders, and notifications in `src/RetailOS.Application/B2B/DTOs/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Database entities, EF Core configurations, migrations, and role-based route gating

- [X] T004 Extend `Product` entity with `decimal? WholesalePrice` (numeric 19,4) and `bool IsWholesaleAvailable = false` in `src/RetailOS.Domain/Entities/Product.cs`
- [X] T005 [P] Create `Merchant` entity in `src/RetailOS.Domain/Entities/Merchant.cs` and `Notification` entity in `src/RetailOS.Domain/Entities/Notification.cs`
- [X] T006 [P] Create `B2BOrder` and `B2BOrderItem` entities with requested/approved quantities, cancellation fields, and credit override audit fields in `src/RetailOS.Domain/Entities/`
- [X] T007 Configure EF Core mappings, StoreId global query filters, and create EF Core migration `AddB2BWholesalePortal` in `src/RetailOS.Infrastructure/`
- [X] T008 Register role `Merchant` in Identity configuration and configure route protection in `src/App.tsx` and `src/components/layout/Sidebar.tsx`

---

## Phase 3: User Story 1 - Merchant Management & Access Authorization (Priority: P1) 🎯 MVP

**Goal**: Store Owners and Managers can register wholesale client shops with credit limits, manage contact details, and create portal login credentials

**Independent Test**: Log in as Owner, open `/merchants`, add a new wholesale merchant with trade name, contact info, and credit limit, and verify merchant and login user are created in the database.

- [X] T009 [US1] Implement `IMerchantService` and `MerchantService` in `src/RetailOS.Application/B2B/MerchantService.cs` creating linked Customer and ApplicationUser records
- [X] T010 [US1] Implement `MerchantsController` in `src/RetailOS.Api/Controllers/MerchantsController.cs` (`GET /api/merchants`, `POST /api/merchants`)
- [X] T011 [P] [US1] Implement API client in `src/features/b2b/api/merchantsApi.ts` and React hook `src/features/b2b/hooks/useMerchants.ts`
- [X] T012 [US1] Implement wholesale merchants table and creation modal in `src/features/b2b/components/MerchantsTable.tsx` and `CreateMerchantModal.tsx`
- [X] T013 [US1] Implement admin Merchants management page in `src/pages/Merchants.tsx`

**Checkpoint**: User Story 1 is fully functional and testable independently as an MVP.

---

## Phase 4: User Story 2 - Wholesale Product Catalog & Merchant Ordering (Priority: P1) 🎯 MVP

**Goal**: Store administrators can designate wholesale prices on products, and authorized merchants can browse the B2B catalog and submit wholesale supply orders

**Independent Test**: Log in as Owner, mark a product as wholesale available with wholesale price 80 EGP (selling price 100 EGP). Log in as Merchant, verify only wholesale-available items appear at 80 EGP without showing cost, assemble a cart, choose payment preference, and submit order `#B2B-1001`.

- [X] T014 [US2] Update Product DTOs, ProductService, and Excel import parser to support optional `WholesalePrice` and `IsWholesaleAvailable` in `src/RetailOS.Application/Products/`
- [X] T015 [US2] Update admin ProductModal in `src/features/products/components/ProductModal.tsx` with "متاح للبيع بالجملة" switch and optional "سعر الجملة" input with expected profit margin
- [X] T016 [US2] Implement B2B catalog endpoint `GET /api/b2b-orders/catalog` in `src/RetailOS.Api/Controllers/B2BOrdersController.cs` exposing `EffectiveWholesalePrice` with zero cost leakage
- [X] T017 [US2] Implement `IB2BOrderService.CreateOrderAsync` in `src/RetailOS.Application/B2B/B2BOrderService.cs` calculating price snapshots on server and assigning `B2B-` order numbers
- [X] T018 [US2] Implement Merchant portal layout, catalog view, and checkout cart in `src/pages/portal/PortalCatalog.tsx` and `src/features/b2b/components/PortalCartModal.tsx`

**Checkpoint**: User Stories 1 and 2 are fully functional and integrated.

---

## Phase 5: User Story 3 - In-App Notification Center & WhatsApp Quick Actions (Priority: P1)

**Goal**: Store Owners and Managers receive instant in-app alerts upon order creation with live badges, and can open pre-composed WhatsApp chat links with the merchant with a single click

**Independent Test**: Submit a B2B order as Merchant. Verify Owner's header notification bell increments in real time, clicking it displays order details, and clicking "فتح محادثة واتساب" opens a pre-composed E.164 message in a new tab without third-party API dependencies.

- [X] T019 [US3] Implement `INotificationService` and `NotificationService` in `src/RetailOS.Application/B2B/NotificationService.cs` generating independent in-app alerts for Owner and Manager
- [X] T020 [US3] Implement `NotificationsController` in `src/RetailOS.Api/Controllers/NotificationsController.cs` (`GET /api/notifications`, `/unread-count`, `POST /api/notifications/{id}/mark-read`)
- [X] T021 [US3] Implement In-App Notification Center with polling and unread counter badge in `src/components/layout/NotificationDropdown.tsx` inside the admin Header
- [X] T022 [US3] Implement E.164 phone normalization and WhatsApp Click-to-Chat URI builder (`https://wa.me/{phone}?text={encoded}`) in `src/features/b2b/utils/whatsappUtils.ts`
- [X] T023 [US3] Add "فتح محادثة واتساب برسالة جاهزة" quick action button in NotificationDropdown and order details for Owner/Manager only

---

## Phase 6: User Story 4 - Order Review, Deficit Adjustment & Invoicing with Credit Override (Priority: P2)

**Goal**: Store Owners can review pending orders, adjust quantities per line if stock is insufficient with audit reasons, approve orders, and convert them to formal Sales Invoices with atomic credit limit override controls

**Independent Test**: Review order `#B2B-1001` requesting 50 units. Adjust approved quantity to 30 with reason "المتاح 30 فقط". Approve order. Click invoice conversion with partial payment; if credit limit is breached, verify 409 error occurs unless explicit override checkbox and 10+ char reason are confirmed; verify atomic sales and inventory ledger posting.

- [X] T024 [US4] Implement `POST /api/b2b-orders/{id}/approve` in `B2BOrderService.cs` with explicit `ApprovedQuantity` per line, server recalculation of total, and mandatory `AdjustmentReason` if reduced
- [X] T025 [US4] Implement atomic `POST /api/b2b-orders/{id}/invoice` in `B2BOrderService.cs` with customer ledger calculation (`outstandingBalance + creditAmount`), stock lock, credit limit check, and mandatory override audit fields if breached
- [X] T026 [US4] Implement admin B2B orders management screen and review modal in `src/pages/B2BOrders.tsx` and `src/features/b2b/components/B2BOrderDetailsModal.tsx` with requested vs approved comparisons and credit override controls

---

## Phase 7: User Story 5 - Order Cancellation & Merchant Account History (Priority: P2)

**Goal**: Merchants can cancel orders while in `Pending` state, reorder items via "إنشاء طلب مشابه", and view their order history and statement of account

**Independent Test**: As Merchant, open `/portal/orders`, click "إلغاء الطلب" on a pending order with confirmation; verify status changes to Cancelled and admin receives notification. Click "إنشاء طلب مشابه" to copy items to a new cart with fresh server pricing.

- [X] T027 [US5] Implement `POST /api/b2b-orders/{id}/cancel` in `B2BOrderService.cs` (atomic transition `Pending` → `Cancelled` with `B2BOrderCancelled` notification)
- [X] T028 [US5] Implement merchant order history and statement of account in `src/pages/portal/PortalOrders.tsx` and `src/pages/portal/PortalStatement.tsx` with cancellation modal and "إنشاء طلب مشابه" reorder utility

---

## Phase 8: Polish & Verification

**Purpose**: Quality assurance, WCAG AA compliance, and production build verification

- [X] T029 [P] Verify WCAG AA contrast (≥ 4.5:1) in both Dark and Light modes across portal catalog, checkout cart, admin tables, and modals
- [X] T030 Run complete production build validation (`npm run build` in `system-FE` and `dotnet build` in `system-BE`) ensuring 0 errors

---

## Phase 9: Operational Refinements & Bug Remediation (Session 2026-09-10)

**Purpose**: Resolve the 6 field issues (statement type error, out-of-stock ordering protection, owner review stock assist, cost & discount capabilities, rejection reason visibility, and change calculation).

- [X] T031 [US5] Fix Account Statement API and Hook contract mismatch in `src/features/b2b/api/merchantsApi.ts`, `src/features/b2b/hooks/useMerchants.ts`, and `src/pages/portal/PortalStatement.tsx`
- [X] T032 [US2] Protect Portal Catalog and Cart from out-of-stock additions and add `expectedDownPayment` input in `src/pages/portal/PortalCatalog.tsx` and `src/features/b2b/components/PortalCartModal.tsx`
- [X] T033 [US4] Update Backend DTOs & B2B Order Service for Down-Payment, Discounts, Stock & Cost in `src/RetailOS.Application/B2B/DTOs/B2BOrderDtos.cs` and `src/RetailOS.Infrastructure/B2B/B2BOrderService.cs`
- [X] T034 [US4] Add Owner Review Assist, Auto-Zero Deficits, and Price/Discount Adjustments in `src/features/b2b/components/B2BOrderDetailsModal.tsx`
- [X] T035 [US4] Implement Cash Overpayment & Dynamic Change Due ("الفكة") Calculation in `src/features/b2b/components/B2BOrderDetailsModal.tsx`
- [X] T036 [US5] Surface Rejection Reason Prominently on Merchant Portal Cards in `src/pages/portal/PortalOrders.tsx`
- [X] T037 Build Validation & Verification (`dotnet build` in `system-BE` and `npm run build` in `system-FE`)


---

## Dependencies & Execution Order

### Phase Dependencies
- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 — blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Phase 2 — delivers MVP foundation.
- **User Story 2 (Phase 4)**: Depends on Phase 2 and Phase 3 — completes B2B self-ordering MVP.
- **User Story 3 (Phase 5)**: Depends on Phase 4.
- **User Story 4 (Phase 6)**: Depends on Phase 4 and Phase 5.
- **User Story 5 (Phase 7)**: Depends on Phase 4.
- **Polish (Phase 8)**: Depends on all user stories.

### Parallel Opportunities
- `T002` and `T003` can run in parallel during Phase 1.
- `T005` and `T006` can run in parallel during Phase 2.
- `T011` and `T012` can run in parallel during Phase 3.
- `T029` and `T030` can run in parallel during Polish.

---

## Implementation Strategy

### MVP First (User Stories 1 & 2)
1. Complete Phase 1 (Setup) + Phase 2 (Foundational entities and migration).
2. Complete Phase 3 (Merchant registration & credentials).
3. Complete Phase 4 (Wholesale catalog pricing and merchant self-ordering).
4. **Validate**: Merchant can log in, view wholesale products at wholesale price, and place a B2B order.

### Incremental Delivery
1. Add Phase 5: In-App notifications and WhatsApp Click-to-Chat quick actions.
2. Add Phase 6: Order review, quantity deficit adjustments, and invoice conversion with credit limit override.
3. Add Phase 7: Order cancellation and merchant statement of account.
4. Polish & Build verification.

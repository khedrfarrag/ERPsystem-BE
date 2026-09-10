# Tasks: Phase 3 — Business Operations

**Branch**: `003-business-operations` | **Date**: 2026-09-02 | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Shared infrastructure, idempotency abstractions, and foundational enums.

- [X] T001 [P] Create domain enums in `src/RetailOS.Domain/Enums/`: `PurchaseStatus.cs`, `SaleStatus.cs`, `SalePaymentMethod.cs`, `RefundMethod.cs`, `SupplierTransactionType.cs`, `CustomerTransactionType.cs`, `PaymentPartyType.cs`, `PaymentMethod.cs`, `CashTransactionType.cs`, `IdempotencyStatus.cs`
- [X] T002 [P] Create `IdempotencyRecord` entity in `src/RetailOS.Domain/Entities/IdempotencyRecord.cs`
- [X] T003 Create `IIdempotencyService` in `src/RetailOS.Application/Common/Interfaces/IIdempotencyService.cs` and implementation in `src/RetailOS.Infrastructure/Services/IdempotencyService.cs`

---

## Phase 2: Foundational (Domain Entities, EF Core Mappings & Migration)

**Purpose**: Core database schema and entity models for all business operations. BLOCKS all user stories.

- [X] T004 [P] Create `Supplier.cs` and `SupplierRepresentative.cs` in `src/RetailOS.Domain/Entities/`
- [X] T005 [P] Create `Customer.cs` in `src/RetailOS.Domain/Entities/Customer.cs`
- [X] T006 [P] Create `SupplierAccountTransaction.cs` and `CustomerAccountTransaction.cs` in `src/RetailOS.Domain/Entities/`
- [X] T007 [P] Create `Purchase.cs`, `PurchaseLineItem.cs`, `PurchaseReturn.cs`, and `PurchaseReturnLineItem.cs` in `src/RetailOS.Domain/Entities/`
- [X] T008 [P] Create `Sale.cs`, `SaleLineItem.cs`, `SaleReturn.cs`, and `SaleReturnLineItem.cs` in `src/RetailOS.Domain/Entities/`
- [X] T009 [P] Create `Payment.cs`, `ExpenseCategory.cs`, `Expense.cs`, and `CashRegisterTransaction.cs` in `src/RetailOS.Domain/Entities/`
- [X] T010 [P] Create EF Core entity configurations for Suppliers, Customers, and Account Transactions in `src/RetailOS.Infrastructure/Persistence/Configurations/`
- [X] T011 [P] Create EF Core entity configurations for Purchases, Sales, Payments, Expenses, Cash Register, and Idempotency in `src/RetailOS.Infrastructure/Persistence/Configurations/`
- [X] T012 Register new `DbSet` properties in `src/RetailOS.Infrastructure/Persistence/AppDbContext.cs` with global query filters
- [X] T013 Create EF Core migration `AddBusinessOperations` and apply to database via `dotnet ef database update`

**Checkpoint**: Database schema and domain entities ready — user story implementations can now begin.

---

## Phase 3: User Story 1 — Supplier & Customer Master Data (Priority: P1) 🎯 MVP

**Goal**: Register and manage suppliers, supplier reps, customers, credit limits, and running account statement ledgers.

**Independent Test**: Register supplier with rep, create customer with credit limit, verify both appear in store listings with zero balance, verify soft-delete guard when linked to transactions.

- [X] T014 [P] [US1] Create Supplier DTOs, interfaces, and FluentValidation validators in `src/RetailOS.Application/Suppliers/`
- [X] T015 [P] [US1] Create Customer DTOs, interfaces, and FluentValidation validators in `src/RetailOS.Application/Customers/`
- [X] T016 [US1] Implement `SupplierService.cs` in `src/RetailOS.Infrastructure/Operations/SupplierService.cs` (CRUD, reps, statement, soft-delete guard)
- [X] T017 [US1] Implement `CustomerService.cs` in `src/RetailOS.Infrastructure/Operations/CustomerService.cs` (CRUD, statement, soft-delete guard)
- [X] T018 [US1] Implement `SuppliersController.cs` and `CustomersController.cs` in `src/RetailOS.Api/Controllers/`
- [X] T019 [US1] Integration tests for Supplier and Customer lifecycle in `tests/RetailOS.IntegrationTests/Operations/SupplierAndCustomerTests.cs`

**Checkpoint**: User Story 1 fully functional and independently verified.

---

## Phase 4: User Story 2 — Purchase Orders & Goods Receipt (Priority: P1)

**Goal**: Record goods receipts from suppliers, calculate line items, update inventory with `Purchase` reason, update product WAC, and update supplier payable ledger.

**Independent Test**: Create draft purchase, modify lines, confirm purchase, verify inventory increases and supplier ledger reflects total invoice amount.

- [X] T020 [P] [US2] Create Purchase DTOs, interfaces, and validators in `src/RetailOS.Application/Purchases/`
- [X] T021 [US2] Implement `PurchaseService.cs` in `src/RetailOS.Infrastructure/Operations/PurchaseService.cs` (Draft creation, mutable edit per Q5, atomic confirmation with WAC update, and purchase returns)
- [X] T022 [US2] Implement `PurchasesController.cs` in `src/RetailOS.Api/Controllers/PurchasesController.cs` with `Idempotency-Key` support
- [X] T023 [US2] Integration tests for Purchase flow, WAC recalculation, purchase return, and idempotency in `tests/RetailOS.IntegrationTests/Operations/PurchaseOrderTests.cs`

**Checkpoint**: User Stories 1 and 2 independently functional and integrated.

---

## Phase 5: User Story 3 — Point of Sale (Sales) (Priority: P1)

**Goal**: Fast POS checkout with pessimistic concurrency locking, stock depletion guard with per-line rejection (per Q1), cash/credit/mixed payment validation (per Q3), WAC COGS calculation, and sale returns at original cost (per Q2).

**Independent Test**: Complete a cash sale and verify stock decrements and COGS recorded; complete a mixed sale with customer credit; attempt oversell and verify entire-sale rejection with detailed line breakdown.

- [X] T024 [P] [US3] Create Sale DTOs, interfaces, and validators in `src/RetailOS.Application/Sales/`
- [X] T025 [US3] Implement `SaleService.cs` in `src/RetailOS.Infrastructure/Operations/SaleService.cs` with pessimistic product locking, stock depletion check, WAC COGS calculation, and customer/cash register ledger entries
- [X] T026 [US3] Implement sale return logic in `SaleService.cs` restoring inventory at original unit cost and reversing COGS
- [X] T027 [US3] Implement `SalesController.cs` in `src/RetailOS.Api/Controllers/SalesController.cs` with `Idempotency-Key` support
- [X] T028 [US3] Integration tests for POS cash, credit, mixed sales, and sale returns in `tests/RetailOS.IntegrationTests/Operations/SalePosTests.cs`
- [X] T029 [US3] Integration tests for concurrent stock depletion and race condition prevention in `tests/RetailOS.IntegrationTests/Operations/ConcurrencyAndStockTests.cs`

**Checkpoint**: User Stories 1, 2, and 3 fully functional and validated under high contention.

---

## Phase 6: User Story 4 — Payments & Account Settlements (Priority: P2)

**Goal**: Record cash and bank settlements from customers or to suppliers, append immutable ledger transactions, and update cash drawer balance.

**Independent Test**: Record customer payment, verify customer balance decreases and cash register increases; test idempotency duplicate protection.

- [X] T030 [P] [US4] Create Payment DTOs, interfaces, and validators in `src/RetailOS.Application/Payments/`
- [X] T031 [US4] Implement `PaymentService.cs` in `src/RetailOS.Infrastructure/Operations/PaymentService.cs` (Customer/Supplier settlements and cash register reflection)
- [X] T032 [US4] Implement `PaymentsController.cs` in `src/RetailOS.Api/Controllers/PaymentsController.cs` with `Idempotency-Key` support
- [X] T033 [US4] Integration tests for payment settlements and idempotency in `tests/RetailOS.IntegrationTests/Operations/PaymentSettlementTests.cs`

**Checkpoint**: User Stories 1 through 4 operational.

---

## Phase 7: User Story 5 — Expenses & Cash Register (Priority: P2)

**Goal**: Operating expenses recording and single shared daily cash register ledger with float opening and end-of-day discrepancy reporting (per Q4).

**Independent Test**: Open cash register with 1000 float, record cash expense of 200, verify balance is 800, perform close count with 780 and verify -20 discrepancy.

- [X] T034 [P] [US5] Create Expense DTOs, interfaces, and validators in `src/RetailOS.Application/Expenses/`
- [X] T035 [P] [US5] Create CashRegister DTOs, interfaces, and validators in `src/RetailOS.Application/CashRegister/`
- [X] T036 [US5] Implement `ExpenseService.cs` in `src/RetailOS.Infrastructure/Operations/ExpenseService.cs`
- [X] T037 [US5] Implement `CashRegisterService.cs` in `src/RetailOS.Infrastructure/Operations/CashRegisterService.cs` (daily single register per store, float, EOD count)
- [X] T038 [US5] Implement `ExpensesController.cs` and `CashRegisterController.cs` in `src/RetailOS.Api/Controllers/`
- [X] T039 [US5] Integration tests for Expenses and Cash Register EOD reconciliation in `tests/RetailOS.IntegrationTests/Operations/CashRegisterAndExpenseTests.cs`

**Checkpoint**: All 5 User Stories implemented and independently verified.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Dependency injection wiring, multi-tenant isolation tests, full regression suite, and module completion gate.

- [X] T040 Register all Phase 3 services and repositories in `src/RetailOS.Infrastructure/DependencyInjection.cs`
- [X] T041 [P] Integration tests verifying Store A's business operations data is completely invisible to Store B in `tests/RetailOS.IntegrationTests/Operations/OperationsTenantIsolationTests.cs`
- [X] T042 Run full regression test suite (`dotnet test`) verifying all Phase 1, Phase 2, and Phase 3 tests pass
- [X] T043 Validate quickstart scenarios end-to-end against `specs/003-business-operations/quickstart.md`
- [X] T044 Verify Definition of Done and Module Completion Gate checklist in `specs/003-business-operations/checklists/requirements.md`

---

## Dependencies & Execution Order

### Phase Dependencies
- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories.
- **User Story 1 (Phase 3)**: Depends on Phase 2. MVP milestone.
- **User Story 2 (Phase 4)**: Depends on Phase 2 and US1 (requires suppliers & products).
- **User Story 3 (Phase 5)**: Depends on Phase 2 and US1/US2 (requires stock & customers).
- **User Story 4 (Phase 6)**: Depends on US1 (customers & suppliers exist to settle).
- **User Story 5 (Phase 7)**: Depends on Phase 2 (cash register ledger). Integrates with US3 and US4 cash flows.
- **Polish (Phase 8)**: Depends on all user stories completed.

---

## Parallel Opportunities

- Within Phase 1: T001 and T002 can run in parallel.
- Within Phase 2: Domain entities (T004-T009) and configurations (T010-T011) can be created in parallel.
- Within User Stories: DTOs and validator tasks marked with `[P]` can be built in parallel.
- In Phase 8: Tenant isolation tests (T041) can be developed alongside wiring (T040).

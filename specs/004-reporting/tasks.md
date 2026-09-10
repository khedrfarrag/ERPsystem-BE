# Tasks: Phase 4 — Reporting & Business Analytics

**Input**: Design artifacts from `specs/004-reporting/` (`spec.md`, `plan.md`, `data-model.md`, `contracts/api-contracts.md`, `quickstart.md`, `research.md`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Application layer contracts, DTO projections, and streaming CSV exporter utility.

- [X] T001 [P] Create Report DTOs in `src/RetailOS.Application/Reports/DTOs/ReportDTOs.cs`
- [X] T002 [P] Create `IReportService.cs` interface in `src/RetailOS.Application/Reports/Interfaces/IReportService.cs`
- [X] T003 [P] Create `CsvExporter.cs` utility in `src/RetailOS.Application/Reports/Common/CsvExporter.cs`

---

## Phase 2: Foundational (Infrastructure & Controller Skeleton)

**Purpose**: Service skeleton, Dependency Injection registration, and API controller foundation.

- [X] T004 Implement `ReportService.cs` skeleton and register in `src/RetailOS.Infrastructure/Operations/ReportService.cs` and `src/RetailOS.Infrastructure/DependencyInjection.cs`
- [X] T005 [P] Implement `ReportsController.cs` in `src/RetailOS.Api/Controllers/ReportsController.cs` with `[Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]`

**Checkpoint**: Infrastructure and controller wiring ready for user story implementations.

---

## Phase 3: User Story 1 — Sales Analytics & Revenue Reporting (Priority: P1) 🎯 MVP

**Goal**: Deliver period sales aggregations, revenue, returns deductions, payment method splits, top-selling products, and category rankings with optional CSV export.

**Independent Test**: Record sales and returns across multiple products and payment methods, query `GET /api/reports/sales`, and verify gross/net sales, returns, and payment breakdowns match exactly.

- [X] T006 [US1] Implement sales summary, product rankings, category breakdown, and payment method split aggregations in `src/RetailOS.Infrastructure/Operations/ReportService.cs`
- [X] T007 [US1] Implement `GET /api/reports/sales` endpoint supporting `from`, `to`, `customerId`, `paymentMethod`, `basis`, and `format=csv` in `src/RetailOS.Api/Controllers/ReportsController.cs`
- [X] T008 [US1] Integration tests for sales summary, product performance, payment breakdowns, and CSV export in `tests/RetailOS.IntegrationTests/Reports/SalesAndProfitReportsTests.cs`

**Checkpoint**: User Story 1 fully functional and independently verified (MVP).

---

## Phase 4: User Story 2 — Profit & Loss (P&L) Statement (Priority: P1)

**Goal**: Deliver P&L calculations subtracting real COGS (from frozen sale line costs) from Net Sales to get Gross Profit, and subtracting Operating Expenses to get Net Profit.

**Independent Test**: Record sales with known unit costs, record operating expenses, query `GET /api/reports/profit-loss`, and verify exact Gross Profit, Net Profit, and margin percentages.

- [X] T009 [US2] Implement P&L mathematical calculation (Gross Sales - Returns - COGS = Gross Profit, - Operating Expenses = Net Profit) in `src/RetailOS.Infrastructure/Operations/ReportService.cs`
- [X] T010 [US2] Implement `GET /api/reports/profit-loss` endpoint in `src/RetailOS.Api/Controllers/ReportsController.cs`
- [X] T011 [US2] Integration tests for P&L math accuracy, zero activity periods, and negative margins in `tests/RetailOS.IntegrationTests/Reports/SalesAndProfitReportsTests.cs`

**Checkpoint**: User Stories 1 and 2 independently functional and verified.

---

## Phase 5: User Story 3 — Inventory Valuation & Stock Movement Reports (Priority: P1)

**Goal**: Deliver real-time store valuation using WAC, point-in-time historical valuation (`asOfDate`), product stock movement chronological audit ledger, and low-stock reorder warnings.

**Independent Test**: Query inventory valuation to verify `SUM(stock * WAC)`, query historical `asOfDate` to verify point-in-time reconstruction, and verify product stock movement tracks every transaction chronologically.

- [X] T012 [US3] Implement real-time inventory valuation and historical `asOfDate` ledger reconstruction in `src/RetailOS.Infrastructure/Operations/ReportService.cs`
- [X] T013 [US3] Implement product stock movement chronological audit ledger and low-stock alerts query in `src/RetailOS.Infrastructure/Operations/ReportService.cs`
- [X] T014 [US3] Implement `GET /api/reports/inventory/valuation`, `GET /api/reports/inventory/movement/{productId}`, and `GET /api/reports/inventory/low-stock` in `src/RetailOS.Api/Controllers/ReportsController.cs`
- [X] T015 [US3] Integration tests for inventory valuation, stock movement history, and low-stock alerts in `tests/RetailOS.IntegrationTests/Reports/InventoryReportsTests.cs`

**Checkpoint**: User Stories 1, 2, and 3 operational and verified.

---

## Phase 6: User Story 4 — Customer & Supplier Account Balances (Priority: P2)

**Goal**: Deliver consolidated accounts receivable and payable reports showing all debtors and creditors with non-zero balances.

**Independent Test**: Query customer and supplier balance reports, verify total receivables and payables match open transaction balances.

- [X] T016 [US4] Implement customer receivables and supplier payables balance queries in `src/RetailOS.Infrastructure/Operations/ReportService.cs`
- [X] T017 [US4] Implement `GET /api/reports/balances/customers` and `GET /api/reports/balances/suppliers` in `src/RetailOS.Api/Controllers/ReportsController.cs`
- [X] T018 [US4] Integration tests for customer and supplier balance aging reports in `tests/RetailOS.IntegrationTests/Reports/BalancesAndCashReportsTests.cs`

**Checkpoint**: User Stories 1 through 4 operational and verified.

---

## Phase 7: User Story 5 — Cash Register Flow & Reconciliation Audit (Priority: P2)

**Goal**: Deliver financial audit reports of the daily cash drawer, including opening floats, operational inflows/outflows, and end-of-day closing discrepancies.

**Independent Test**: Perform daily cash operations with float and discrepancy, query `GET /api/reports/cash-register`, and verify daily net cash flow and discrepancy totals.

- [X] T019 [US5] Implement cash drawer flows, opening floats, operational inflows/outflows, and EOD closing discrepancies query in `src/RetailOS.Infrastructure/Operations/ReportService.cs`
- [X] T020 [US5] Implement `GET /api/reports/cash-register` endpoint in `src/RetailOS.Api/Controllers/ReportsController.cs`
- [X] T021 [US5] Integration tests for cash register flow and EOD reconciliation audit in `tests/RetailOS.IntegrationTests/Reports/BalancesAndCashReportsTests.cs`

**Checkpoint**: All 5 User Stories implemented and independently verified.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Security RBAC enforcement, multi-tenant isolation verification, full regression suite, and module completion gate.

- [X] T022 [P] Integration tests verifying Cashier role is forbidden from all financial reports in `tests/RetailOS.IntegrationTests/Reports/ReportsSecurityAndIsolationTests.cs`
- [X] T023 [P] Integration tests verifying complete multi-tenant data isolation across reporting endpoints in `tests/RetailOS.IntegrationTests/Reports/ReportsSecurityAndIsolationTests.cs`
- [X] T024 Run full regression test suite (`dotnet test`) verifying all Phase 1-4 tests pass
- [X] T025 Validate quickstart scenarios end-to-end against `specs/004-reporting/quickstart.md`
- [X] T026 Verify Definition of Done and Module Completion Gate checklist in `specs/004-reporting/checklists/requirements.md`

---

## Dependencies & Execution Order

### Phase Dependencies
- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories.
- **User Story 1 (Phase 3)**: Depends on Phase 2. MVP milestone.
- **User Story 2 (Phase 4)**: Depends on Phase 2 & US1.
- **User Story 3 (Phase 5)**: Depends on Phase 2.
- **User Story 4 (Phase 6)**: Depends on Phase 2.
- **User Story 5 (Phase 7)**: Depends on Phase 2.
- **Polish & Isolation (Phase 8)**: Depends on all user stories being complete.

---

## Implementation Strategy

### MVP First (User Story 1 Only)
1. Complete Phase 1: Setup (`T001–T003`)
2. Complete Phase 2: Foundational (`T004–T005`)
3. Complete Phase 3: User Story 1 (`T006–T008`)
4. **Validate MVP**: Test sales analytics, top products, and CSV export.

### Incremental Delivery
1. Add User Story 2: P&L Statement (`T009–T011`)
2. Add User Story 3: Inventory Valuation & Stock Movement (`T012–T015`)
3. Add User Story 4: Balances & Aging (`T016–T018`)
4. Add User Story 5: Cash Register Flow Audit (`T019–T021`)
5. Finalize Phase 8: Security, Tenant Isolation, and Full Suite Regression (`T022–T026`)

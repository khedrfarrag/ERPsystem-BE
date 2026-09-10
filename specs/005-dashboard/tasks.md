# Tasks: Phase 5 — Dashboard & Real-Time KPIs

**Branch**: `005-dashboard` | **Spec**: [specs/005-dashboard/spec.md](file:///g:/system-analysiss-saas/system-BE/specs/005-dashboard/spec.md) | **Plan**: [specs/005-dashboard/plan.md](file:///g:/system-analysiss-saas/system-BE/specs/005-dashboard/plan.md)

---

## Phase 1: Setup & Contracts

- [X] T001 [P] Create DTO records (`DashboardSummaryDto.cs`, `SalesTrendDto.cs`, `TopProductDto.cs`, `SlowMovingProductDto.cs`, `LowStockAlertDto.cs`, `RecentActivityDto.cs`) in `src/RetailOS.Application/Dashboard/DTOs/`
- [X] T002 [P] Define `IDashboardService.cs` interface with all query methods in `src/RetailOS.Application/Dashboard/IDashboardService.cs`
- [X] T003 Register `IDashboardService` and its implementation in `src/RetailOS.Infrastructure/DependencyInjection.cs`

---

## Phase 2: Foundational Infrastructure

- [X] T004 Create `DashboardService.cs` skeleton in `src/RetailOS.Infrastructure/Operations/DashboardService.cs` injecting `AppDbContext`, `IStoreContext`, and logging
- [X] T005 Create `DashboardController.cs` in `src/RetailOS.Api/Controllers/DashboardController.cs` with `[Authorize(Roles = "Owner,Manager")]` attribute and standard `ApiResponse<T>` envelope wrapper

---

## Phase 3: User Story 1 (P1) - Real-Time Store Summary KPI Cards

*Goal: Deliver real-time executive store pulse (Today's revenue, order counts, cash vs credit split, operating profit, live drawer cash, receivables, payables, stock counters).*

- [X] T006 [US1] Implement `GetSummaryAsync` in `src/RetailOS.Infrastructure/Operations/DashboardService.cs` computing today's sales, gross/operating profit, live cash drawer status/balance, customer receivables, supplier payables, and low stock counts
- [X] T007 [US1] Implement endpoint `GET /api/dashboard/summary` in `src/RetailOS.Api/Controllers/DashboardController.cs`
- [X] T008 [US1] Add integration tests for `GET /api/dashboard/summary` in `tests/RetailOS.IntegrationTests/Dashboard/DashboardTests.cs` verifying calculation accuracy against recorded transactions

---

## Phase 4: User Story 2 (P2) - Sales Trend & Period Charts

*Goal: Deliver continuous time-series sales trend metrics over configurable periods with zero-filled gaps.*

- [X] T009 [US2] Implement `GetSalesTrendAsync` in `src/RetailOS.Infrastructure/Operations/DashboardService.cs` supporting `days=7`, `days=30`, or custom date ranges and zero-filling days with no sales
- [X] T010 [US2] Implement endpoint `GET /api/dashboard/sales-trend` in `src/RetailOS.Api/Controllers/DashboardController.cs`
- [X] T011 [US2] Add integration tests for `GET /api/dashboard/sales-trend` in `tests/RetailOS.IntegrationTests/Dashboard/DashboardTests.cs` validating zero-gap chronological output

---

## Phase 5: User Story 3 (P3) - Inventory Movement Insights (Top-Selling & Slow-Moving Products)

*Goal: Identify best-selling products by volume/revenue and detect stagnant items tying up working capital.*

- [X] T012 [US3] Implement `GetTopProductsAsync` in `src/RetailOS.Infrastructure/Operations/DashboardService.cs` ranking top products by quantity sold and revenue within period
- [X] T013 [US3] Implement `GetSlowMovingProductsAsync` in `src/RetailOS.Infrastructure/Operations/DashboardService.cs` identifying active items with `Stock > 0` and zero sales over inactivity window
- [X] T014 [US3] Implement endpoints `GET /api/dashboard/top-products` and `GET /api/dashboard/slow-moving-products` in `src/RetailOS.Api/Controllers/DashboardController.cs`
- [X] T015 [US3] Add integration tests for top and slow-moving products in `tests/RetailOS.IntegrationTests/Dashboard/DashboardTests.cs`

---

## Phase 6: User Story 4 (P4) - Critical Inventory Alerts & Recent Activity Feed

*Goal: Alert managers of items at or below reorder threshold and stream live store operations.*

- [X] T016 [US4] Implement `GetLowStockAlertsAsync` in `src/RetailOS.Infrastructure/Operations/DashboardService.cs` filtering items where `CurrentStock <= MinStockLevel` ordered by deficit
- [X] T017 [US4] Implement `GetRecentActivityAsync` in `src/RetailOS.Infrastructure/Operations/DashboardService.cs` aggregating latest sales, purchases, and expenses
- [X] T018 [US4] Implement endpoints `GET /api/dashboard/low-stock-alerts` and `GET /api/dashboard/recent-activity` in `src/RetailOS.Api/Controllers/DashboardController.cs`
- [X] T019 [US4] Add integration tests for low-stock alerts and recent activity feed in `tests/RetailOS.IntegrationTests/Dashboard/DashboardTests.cs`

---

## Phase 7: Polish, Security & Cross-Cutting Concerns

- [X] T020 Add integration tests in `tests/RetailOS.IntegrationTests/Dashboard/DashboardTests.cs` verifying `Cashier` and `InventoryClerk` receive `403 Forbidden` on dashboard endpoints
- [X] T021 Add multi-tenant isolation tests in `tests/RetailOS.IntegrationTests/Dashboard/DashboardTests.cs` ensuring Store A sees zero metrics from Store B
- [X] T022 Execute full test suite (`dotnet test`) across all modules (Phases 1-5) and confirm 100% pass rate

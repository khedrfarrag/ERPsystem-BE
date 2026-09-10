# Implementation Plan: Phase 4 — Reporting & Business Analytics

**Branch**: `004-reporting` | **Date**: 2026-09-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-reporting/spec.md`

---

## Summary

Phase 4 delivers the comprehensive business reporting and analytics layer for Retail OS. It aggregates transactional, inventory, and ledger history across 5 major domains:
1. **Sales & Revenue Analytics**: Period summaries, payment method breakdowns, top-selling products, and returns impact.
2. **Profit & Loss (P&L) Statements**: Gross profit derived from real COGS, operating expenses, and net margins.
3. **Inventory & Valuation**: Real-time store valuation by WAC, point-in-time historical valuation, product stock movement ledger, and low-stock alerts.
4. **Accounts Balances & Aging**: Accounts receivable and payable summaries.
5. **Cash Flow & Register Audit**: Daily opening float, net operational cash flow, and end-of-day count discrepancy tracking.

---

## Technical Context

**Language/Version**: .NET 9.0 / C# 13  
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 9.0, Npgsql.EntityFrameworkCore.PostgreSQL  
**Storage**: PostgreSQL 18 with snake_case naming convention (Existing tables: `sales`, `sale_line_items`, `sale_returns`, `inventory_transactions`, `expenses`, `payments`, `cash_register_transactions`, `products`)  
**Testing**: xUnit, FluentAssertions, `WebApplicationFactory<Program>` Integration Tests  
**Target Platform**: Windows / Linux Docker  
**Project Type**: Web API (Modular Monolith)  
**Performance Goals**: Report queries execute in < 500ms for stores with 100k transactions  
**Constraints**: Multi-tenant data isolation via `IStoreContext` & EF Core Global Query Filters; Role-Based Access Control restricting financial reports to `Owner` and `Manager` roles; Streaming CSV generation with zero third-party heavy dependencies.

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Multi-Tenant Isolation**: Verified. All reporting queries automatically inherit EF Core `store_id` query filters.
- **Snake_Case Database**: Verified. All queries map cleanly to existing PostgreSQL snake_case tables.
- **Role-Based Access Control**: Verified. All `/api/reports/*` endpoints are annotated with `[Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]`. Cashiers are strictly denied.
- **Financial Precision**: Verified. Monetary calculations strictly use `decimal` (C#) and `numeric(18,2)` / `numeric(19,6)` in PostgreSQL. Division-by-zero is strictly guarded.

---

## Project Structure

### Documentation (`specs/004-reporting/`)

```text
specs/004-reporting/
├── spec.md              # Feature specification with clarifications
├── checklists/
│   └── requirements.md  # Specification quality checklist (16/16 verified)
├── research.md          # Architecture decisions, P&L formulation, streaming CSV
├── data-model.md        # Report request parameters and response DTO definitions
├── quickstart.md        # 6 end-to-end integration validation scenarios
├── contracts/
│   └── api-contracts.md # HTTP REST endpoints specification
├── plan.md              # Implementation plan (this document)
└── tasks.md             # Task breakdown (generated via /speckit-tasks)
```

### Source Code (`src/` and `tests/`)

```text
src/
├── RetailOS.Application/
│   └── Reports/
│       ├── DTOs/
│       │   └── ReportDTOs.cs
│       ├── Interfaces/
│       │   └── IReportService.cs
│       └── Common/
│           └── CsvExporter.cs
├── RetailOS.Infrastructure/
│   └── Operations/
│       └── ReportService.cs
└── RetailOS.Api/
    └── Controllers/
        └── ReportsController.cs

tests/
└── RetailOS.IntegrationTests/
    └── Reports/
        ├── SalesAndProfitReportsTests.cs
        ├── InventoryReportsTests.cs
        ├── BalancesAndCashReportsTests.cs
        └── ReportsSecurityAndIsolationTests.cs
```

---

## Layered Implementation Strategy

1. **Application Layer (`RetailOS.Application`)**:
   - Define all strongly-typed Report DTOs (`SalesReportResponse`, `ProfitLossReportResponse`, `InventoryValuationReportResponse`, `StockMovementLedgerResponse`, `CashRegisterAuditReportResponse`, etc.).
   - Define `IReportService` interface with all query methods.
   - Implement `CsvExporter` helper for zero-dependency streaming CSV conversion.

2. **Infrastructure Layer (`RetailOS.Infrastructure`)**:
   - Implement `ReportService` using optimized `.AsNoTracking()` LINQ queries and server-side PostgreSQL aggregations (`SUM`, `COUNT`, `GroupBy`).
   - Implement historical point-in-time stock reconstruction via `inventory_transactions` ledger when `asOfDate` is specified.
   - Register `IReportService` in DI container (`DependencyInjection.cs`).

3. **Presentation / API Layer (`RetailOS.Api`)**:
   - Create `ReportsController` under `/api/reports/` with Swagger annotations and role checks (`[Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]`).
   - Support `format=csv` query parameter for returning `File(bytes, "text/csv", filename)`.

4. **Integration Test Suite (`RetailOS.IntegrationTests`)**:
   - Verify sales summaries, P&L formula accuracy, WAC valuation, product stock movement audit trails, accounts balance aging, CSV exports, cashier role denials, and multi-tenant isolation.

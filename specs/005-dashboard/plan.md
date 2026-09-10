# Implementation Plan: Phase 5 — Dashboard & Real-Time KPIs

**Branch**: `005-dashboard` | **Date**: 2026-09-05 | **Spec**: [specs/005-dashboard/spec.md](file:///g:/system-analysiss-saas/system-BE/specs/005-dashboard/spec.md)

**Input**: Feature specification from `/specs/005-dashboard/spec.md`

## Summary
Implement high-performance, real-time dashboard analytics and executive summary KPIs for RetailOS. The module delivers 6 dedicated endpoints under `/api/dashboard` providing:
1. Store summary pulse cards (Today's revenue, order counts, gross/operating profit, live cash drawer balance, customer receivables, supplier payables, and low stock counters).
2. Zero-gap sales trend charts over configurable periods (`days=7` or `days=30` or custom range).
3. Top-selling products ranking by quantity and revenue.
4. Slow-moving (dead stock) product identification with tied-up capital calculations.
5. Critical low-stock reorder alerts (`Stock <= MinStockLevel`).
6. Recent store operations activity stream.

All calculations strictly use immutable recorded transactions, non-tracking EF Core queries (`AsNoTracking()`), exact `decimal` precision, and RBAC authorization (`Owner` and `Manager` only).

---

## Technical Context

**Language/Version**: C# 13 / .NET 9.0  
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 9.0, Npgsql.EntityFrameworkCore.PostgreSQL  
**Storage**: PostgreSQL 16+ (existing database `detergents_shop`)  
**Testing**: xUnit, FluentAssertions, WebApplicationFactory integration test suite  
**Target Platform**: Windows / Linux server (Modular Monolith)  
**Project Type**: REST Web API  
**Performance Goals**: < 150ms response time on all dashboard endpoints  
**Constraints**: Zero-gap date series, decimal financial precision, strict tenant isolation via `IStoreContext` & global query filters, RBAC 403 enforcement for non-manager roles  
**Scale/Scope**: Real-time aggregation over active store transactions  

---

## Constitution Check

| Principle / Rule | Compliance Status | Details |
|:---|:---|:---|
| **Absolute: Correctness** | **PASS** | Profits derived from `Revenue - COGS = Gross Profit` and `Gross Profit - Expenses = Operating Profit`. Zero estimated or made-up numbers. |
| **Absolute: Security** | **PASS** | `[Authorize(Roles = "Owner,Manager")]` applied. Cashiers receive `403 Forbidden`. |
| **Absolute: Tenant Isolation** | **PASS** | All queries enforce `StoreId` global query filter + `_storeContext.GetCurrentStoreId()`. |
| **KISS & YAGNI** | **PASS** | Lightweight LINQ projections over active tables. No premature caching or event-sourcing abstractions. |
| **Monetary & Rounding** | **PASS** | All currency fields use `decimal` with `MidpointRounding.AwayFromZero`. |
| **Read Performance** | **PASS** | `AsNoTracking()` and targeted `.Select()` projections used across all queries. |

---

## Project Structure

### Documentation (this feature)

```text
specs/005-dashboard/
├── spec.md              # Feature specification
├── plan.md              # Implementation plan (this file)
├── research.md          # Architecture & query optimization research
├── data-model.md        # DTO models & IDashboardService contract
├── contracts/           # API contract definitions
│   └── api-contracts.md
├── quickstart.md        # Verification guide
└── checklists/
    └── requirements.md  # Quality checklist
```

### Source Code

```text
src/
├── RetailOS.Application/
│   └── Dashboard/
│       ├── IDashboardService.cs
│       └── DTOs/
│           ├── DashboardSummaryDto.cs
│           ├── SalesTrendDto.cs
│           ├── TopProductDto.cs
│           ├── SlowMovingProductDto.cs
│           ├── LowStockAlertDto.cs
│           └── RecentActivityDto.cs
├── RetailOS.Infrastructure/
│   ├── Operations/
│   │   └── DashboardService.cs
│   └── DependencyInjection.cs
├── RetailOS.Api/
│   └── Controllers/
│       └── DashboardController.cs
tests/
└── RetailOS.IntegrationTests/
    └── Dashboard/
        └── DashboardTests.cs
```

---

## Complexity Tracking

No constitutional violations or unnecessary complexity introduced.

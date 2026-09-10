# Research & Architecture Decisions: Phase 5 — Dashboard & Real-Time KPIs

## Context & Objectives
The Dashboard is the executive entry point of RetailOS. Store Owners and Managers require instant (< 150ms) visibility into daily operational metrics, cash positions, profit indicators, stock alerts, and sales momentum. The system must compute these metrics deterministically from active transactional data while keeping database load minimal.

---

## Technical Decisions & Rationale

### 1. High-Performance Read-Only Aggregation Strategy
- **Decision**: Use `AsNoTracking()` with direct EF Core LINQ projections (`Select`) for aggregated summaries and list queries.
- **Rationale**: Dashboard queries do not mutate state. Direct projection avoids loading full entity graphs and tracking overhead, enabling PostgreSQL to execute index-only or lightweight aggregation scans in single-digit milliseconds.
- **Alternatives Considered**:
  - *Materialized Views / Background Aggregation Tables*: Rejected per YAGNI principle (Constitution Principle II). For typical retail store data volumes, single indexed queries over active tenant partitions are sub-10ms without stale data synchronization risks.
  - *In-Memory Caching (Redis)*: Prohibited by Constitution (Principle II & Out of Scope).

### 2. Time-Series Date Bucketing (Zero-Gap Daily Trend)
- **Decision**: Query sales grouped by date (`sale.CreatedAt.Date`) in the database, then fill missing calendar dates in C# memory across the requested date window (`days=7` or `days=30` or custom range).
- **Rationale**: SQL `GROUP BY date` only returns days with sales. A pure SQL calendar table or `generate_series` is complex across EF Core providers. Projecting aggregated daily points and merging them into a complete chronological date range in application memory guarantees zero-gap data points with $O(N)$ speed where $N \le 30$.
- **Alternatives Considered**:
  - *Returning only days with transactions*: Breaks UI line/bar charts because missing days distort time axes.

### 3. Real-Time Cash Drawer Integration
- **Decision**: Query the current active open cash register session for the store (`CashRegisterSessions.FirstOrDefaultAsync(s => s.Status == "OPEN")`).
- **Rationale**: If a session is open, calculate live cash as:
  $$\text{Live Drawer Cash} = \text{OpeningFloat} + \text{OperationalCashIn} - \text{OperationalCashOut} + \text{TodayCashSales}$$
  If no session is open, return `isOpen: false` with the last closed session balance or 0.
- **Alternatives Considered**:
  - *Hardcoding cash balance or requiring manual calculation*: Violates business integrity (Principle VI).

### 4. Slow-Moving (Dead Stock) Detection Algorithm
- **Decision**: Query active products where `CurrentStock > 0`, perform a left join / subquery against `SaleItems` for sales within the threshold window (`CreatedAt >= cutoffDate`), and filter for items with zero sold quantity. Order descending by on-hand capital value (`CurrentStock * PurchasePrice` or WAC).
- **Rationale**: This gives managers immediate actionable insight into locked-up capital that is not rotating.
- **Alternatives Considered**:
  - *Calculating inventory turnover ratio (ITR)*: Overly complex for a small retail shop MVP; a direct list of stagnant products with positive stock is immediately actionable.

### 5. Multi-Tenant Isolation & Role Authorization
- **Decision**: Apply `[Authorize(Roles = "Owner,Manager")]` to `DashboardController`. Enforce automatic store filtering via `_storeContext.GetCurrentStoreId()` and EF Core global query filters.
- **Rationale**: Satisfies Absolute Constraints (Security & Tenant Isolation) and Role-Based Access Control rules (Cashiers/InventoryClerks get `403 Forbidden` on executive dashboard).

---

## Summary of Architectural Constraints
1. Monetary precision: `decimal` with commercial rounding (`AwayFromZero`).
2. No tracking (`AsNoTracking()`) on all dashboard queries.
3. Strict tenant isolation (`StoreId`).
4. Response time target: $< 150\text{ms}$.

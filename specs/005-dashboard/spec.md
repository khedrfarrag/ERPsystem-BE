# Feature Specification: Phase 5 — Dashboard & Real-Time KPIs

**Feature Branch**: `005-dashboard`

**Created**: 2026-09-05

**Status**: Draft

**Input**: User description: "Phase 5 — Dashboard"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Real-Time Store Summary KPI Cards (Priority: P1)

As a Store Owner or Manager, I want to open the system home screen and immediately see real-time, consolidated business KPI cards (Today's Sales Revenue, Orders Count, Current Cash Drawer balance, Operating Profit, Stock Alerts count, and Outstanding Balances) so that I can assess the financial and operational pulse of my store in seconds without running heavy report queries.

**Why this priority**: Summary KPIs represent the single most viewed screen by business owners. High-performance, pre-aggregated overview figures give immediate visibility into daily performance and operational liquidity.

**Independent Test**: Can be tested by executing sales, cash movements, and expenses for a store and verifying that the summary dashboard endpoint returns precise, real-time aggregated metrics matching the underlying transactional state.

**Acceptance Scenarios**:

1. **Given** an authenticated Store Owner with completed sales today totaling 1,500 EGP (1,000 Cash, 500 Credit) across 5 orders, **When** requesting the dashboard summary, **Then** the response shows `todaySalesRevenue = 1500`, `todaySalesCount = 5`, `todayCashSales = 1000`, `todayCreditSales = 500`.
2. **Given** an active open cash register with 200 EGP opening float and 1,000 EGP cash sales, **When** requesting the dashboard summary, **Then** `cashDrawerBalance` reflects the current live operational drawer cash.
3. **Given** existing customer balances of 3,200 EGP and supplier balances of 4,500 EGP, **When** requesting the dashboard summary, **Then** `totalReceivables` is 3200 and `totalPayables` is 4500.

---

### User Story 2 - Sales Trend & Period Charts (Priority: P2)

As a Store Owner or Manager, I want to view a visual time-series breakdown of sales revenue, order counts, and profit trends over recent days (e.g., last 7 or 30 days) so that I can observe sales momentum, identify peak days, and track revenue trajectory.

**Why this priority**: Visual historical trends allow managers to make informed staffing and purchasing decisions based on daily demand swings.

**Independent Test**: Can be tested by generating sales across multiple calendar days within the target window and verifying that daily bucketed aggregates return accurate chronological date-by-date data points with no missing days (zero-filled days included).

**Acceptance Scenarios**:

1. **Given** sales recorded on 4 out of the last 7 calendar days, **When** querying sales trend for the last 7 days, **Then** exactly 7 consecutive daily data points are returned, with zero revenue and zero orders for days without transactions.
2. **Given** a request specifying a trend period, **When** calculating daily buckets, **Then** revenue and gross profit are computed per day in chronological order.

---

### User Story 3 - Inventory Movement Insights (Top-Selling & Slow-Moving Products) (Priority: P3)

As a Store Manager, I want to see the top-selling products by quantity/revenue as well as slow-moving (dead stock) items so that I can reorder hot items proactively and liquidate or discount stagnant inventory.

**Why this priority**: Inventory turnover optimization directly impacts cash flow; knowing what sells fastest and what is stagnating prevents stockouts and tied-up capital.

**Independent Test**: Can be tested by checking product sales histories and verifying that the top N products are ordered descending by volume/revenue, and slow-moving items list active products with stock on hand that have zero or lowest sales over the evaluation window.

**Acceptance Scenarios**:

1. **Given** multiple product sales over the last 30 days, **When** requesting top-selling products with `limit=5`, **Then** the 5 products with the highest quantity sold are returned with their units sold, revenue, and current stock level.
2. **Given** active products with positive stock that have had zero sales in the last 30 days, **When** requesting slow-moving products, **Then** those items are identified with their current on-hand stock and days since last sold.

---

### User Story 4 - Critical Inventory Alerts & Recent Activity Feed (Priority: P4)

As an Inventory Manager or Cashier, I want to see an immediate list of products at or below their reorder threshold (`Stock <= MinStockLevel`) and a mini-feed of the most recent store transactions (recent sales, purchases, expenses) so that I can take immediate corrective actions during daily operations.

**Why this priority**: Actionable operational alerts prevent stockouts and provide live auditing of store checkout activities without browsing multiple sub-pages.

**Independent Test**: Can be tested by lowering product stock below `MinStockLevel` and verifying inclusion in low-stock alerts, and creating new sales/expenses and checking their appearance in the recent activities list.

**Acceptance Scenarios**:

1. **Given** 3 products where `CurrentStock <= MinStockLevel`, **When** requesting low-stock dashboard alerts, **Then** all 3 products are returned with current stock, threshold, and unit name.
2. **Given** 5 recently completed sales and expenses, **When** fetching recent store activities, **Then** the list returns the latest transactions with timestamps, actor/cashier name, and monetary amounts.

---

### Edge Cases

- **Store with Zero Transactions**: Freshly created stores with no sales, purchases, or expenses must return clean 0 values without throwing null reference or divide-by-zero exceptions.
- **Days with No Sales in Trend**: Daily trend arrays must include every calendar day in the requested range, populating 0 for days with no activity.
- **Products with Negative or Zero MinStockLevel**: Only active products with legitimate low-stock conditions are flagged in alerts.
- **Timezone Alignment**: Daily KPI calculations (e.g., "Today's Sales") must respect the store's configured local date/time boundary rather than drifting due to UTC midnight shifts.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an endpoint `GET /api/dashboard/summary` returning real-time aggregated KPI metrics for the store (Today's Sales Revenue, Today's Orders Count, Today's Cash vs Credit split, Estimated Daily Operating Profit, Current Live Cash Drawer balance, Low Stock Count, Total Receivables, Total Payables).
- **FR-002**: System MUST calculate all monetary and profit KPI values using exact `decimal` calculations and immutable recorded transactions (no estimated or fabricated figures).
- **FR-003**: System MUST provide an endpoint `GET /api/dashboard/sales-trend` returning daily time-series sales data points (date, total sales revenue, order count, gross profit). It MUST support `days=7` (default) or `days=30`, or custom `startDate` and `endDate`, zero-filling any dates without sales.
- **FR-004**: System MUST provide an endpoint `GET /api/dashboard/top-products` returning the top N best-selling products by quantity and revenue over a specified time window (`days=30` default, configurable `limit=5` or `10`), including current on-hand stock balance.
- **FR-005**: System MUST provide an endpoint `GET /api/dashboard/slow-moving-products` returning active products with positive inventory on hand (`Stock > 0`) that have had zero sales recorded within a specified inactivity window (`days=30` default, configurable), returning stock on hand, valuation, and days since last sale.
- **FR-006**: System MUST provide an endpoint `GET /api/dashboard/low-stock-alerts` returning active products whose `CurrentStock <= MinStockLevel`, ordered by criticality (lowest stock ratio / highest deficit first).
- **FR-007**: System MUST provide an endpoint `GET /api/dashboard/recent-activity` returning the latest N recorded store operations (sales, purchases, expenses) with transaction type, reference number, amount, created timestamp, and actor username.
- **FR-008**: System MUST enforce Role-Based Access Control on dashboard endpoints, restricting access to `Owner` and `Manager` roles and returning `403 Forbidden` for unauthorized roles (`Cashier`, `InventoryClerk`).
- **FR-009**: System MUST strictly isolate all dashboard metrics and data to the authenticated tenant (`StoreId`) derived from JWT claims.
- **FR-010**: All dashboard read queries MUST use non-tracking queries (`AsNoTracking()`) and optimized projections to achieve near-instantaneous response times (< 150ms).

---

### Key Entities *(include if feature involves data)*

- **DashboardSummaryDto**: Aggregated object containing high-level overview cards (Today's sales, profit, cash drawer, receivables, payables, alert counters).
- **SalesTrendPointDto**: Single daily data point in the trend series (Date, TotalSales, OrderCount, GrossProfit).
- **TopProductDto**: Product ranking record (ProductId, Name, Barcode, UnitsSold, TotalRevenue, CurrentStock, UnitName).
- **SlowMovingProductDto**: Stagnant product record (ProductId, Name, CurrentStock, EstimatedStockValue, LastSoldDate, DaysInactive).
- **LowStockAlertDto**: Critical stock item (ProductId, Name, CurrentStock, MinStockLevel, UnitName, DeficitQuantity).
- **RecentActivityDto**: Live activity item (Id, TransactionType, ReferenceNumber, Amount, CreatedAt, CreatedBy).

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Dashboard summary metrics load in under 150 milliseconds under standard store data volumes.
- **SC-002**: 100% mathematical consistency between dashboard KPI figures and the formal financial / sales reports generated in Phase 4.
- **SC-003**: Trend charts render 100% complete time-series without gaps or missing calendar days for any valid date range.
- **SC-004**: Multi-tenant data leakage rate is 0.0% — zero cross-store metric contamination verified across all queries.
- **SC-005**: Store managers can identify stock shortages and daily revenue trends from a single screen in less than 5 seconds.

---

## Assumptions

- Store timezone is standardized or UTC-aligned with client date filtering for daily rollups.
- Dashboard endpoints are read-only and designed for high-frequency polling or initial page-load consumption.
- Gross profit and operating profit models adhere strictly to the RetailOS Constitution (`Revenue - COGS = Gross Profit`, `Gross Profit - Operating Expenses = Operating Profit`).
- Fast database indexes on `(store_id, created_at)` and `(store_id, is_active)` exist from previous phases.

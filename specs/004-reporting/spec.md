# Feature Specification: Phase 4 — Reporting & Business Analytics

**Feature Branch**: `004-reporting`  
**Created**: 2026-09-05  
**Status**: Draft  
**Input**: User description: "Phase 4 — Reporting"

## Clarifications

### Session 2026-09-05
- Q: Should Sales & P&L reports calculate revenue on an Accrual basis or Cash basis? → A: Option A (Accrual basis as the default accounting standard, with an optional query toggle `basis=Cash|Accrual`).
- Q: How should the Inventory Valuation report calculate stock values? → A: Option B (Support real-time valuation by default, and support an optional `asOfDate` parameter to reconstruct historical point-in-time stock and valuation from the inventory ledger).
- Q: What format(s) should the Reporting API endpoints support for delivering report data? → A: Option A (Structured JSON responses by default for frontend dashboards, with direct CSV export support via `format=csv` for tabular data).

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sales Analytics & Revenue Reporting (Priority: P1) 🎯 MVP

Store Owners and Managers need comprehensive sales reporting over selectable date ranges (today, this week, this month, custom interval) with breakdowns by product, category, cashier, and payment method (Cash, Credit, Mixed), accounting for returned items.

**Why this priority**: Sales reporting is the fundamental measure of store commercial health and immediate turnover.

**Independent Test**: Record several sales and returns with varying payment methods and products, then query the sales summary endpoint to verify accurate total revenue, return deductions, product sales quantities, and payment method splits.

**Acceptance Scenarios**:
1. **Given** a date range with 5 completed sales totaling $1,000 and 1 sale return of $100, **When** requesting the sales report, **Then** the report shows Gross Sales $1,000, Returns $100, Net Sales $900, and transaction count of 5.
2. **Given** sales split across Cash ($600) and Customer Credit ($400), **When** requesting the sales report by payment method, **Then** the breakdown precisely reflects Cash ($600) and Credit ($400).
3. **Given** sales of multiple products, **When** requesting product performance analytics, **Then** products are ranked by quantity sold and revenue generated.

---

### User Story 2 - Profit & Loss (P&L) Statement (Priority: P1)

Store Owners need to analyze profitability by calculating Net Sales Revenue minus Cost of Goods Sold (COGS based on original sale item unit costs) to determine Gross Profit, minus Operating Expenses to determine Net Profit and Profit Margin percentage.

**Why this priority**: Accurate profit calculation is essential to understanding whether the store is operating profitably after covering merchandise costs and operational overhead.

**Independent Test**: Execute sales with known unit costs, record operational expenses for the period, and verify that the P&L report generates Gross Profit = Net Revenue - COGS, and Net Profit = Gross Profit - Total Expenses.

**Acceptance Scenarios**:
1. **Given** Net Sales of $1,000 with COGS of $600 and Operating Expenses of $150, **When** generating the P&L report, **Then** Gross Profit is $400 (40.0% gross margin) and Net Profit is $250 (25.0% net margin).
2. **Given** a period with zero sales but $200 in operating expenses, **When** generating the P&L report, **Then** Gross Profit is $0 and Net Profit is -$200 (Net Loss).

---

### User Story 3 - Inventory Valuation & Stock Movement Reports (Priority: P1)

Store Owners and Managers need visibility into total stock value based on Weighted Average Cost (WAC), low-stock warning lists, and item-by-item movement audit cards (tracking all stock additions, deductions, sales, purchases, returns, and adjustments).

**Why this priority**: Inventory is the primary capital asset of a retail store; managers need to audit movements and prevent stockouts.

**Independent Test**: Generate the inventory valuation report to verify total asset value across all items matches `SUM(current_stock * purchase_cost)`, and verify an individual product's stock movement ledger returns chronological transaction history.

**Acceptance Scenarios**:
1. **Given** 100 units of Product A (WAC $5) and 50 units of Product B (WAC $20), **When** requesting inventory valuation, **Then** total inventory value is $1,500.
2. **Given** a product with opening stock, a purchase, a sale, and a return, **When** requesting the product's stock movement report, **Then** all 4 events appear chronologically with respective quantities, costs, and resulting balances.
3. **Given** products with stock below their minimum reorder threshold, **When** requesting low stock alerts, **Then** only depleted products are returned with current vs reorder levels.

---

### User Story 4 - Customer & Supplier Account Balances (Priority: P2)

Store Owners need consolidated debtor and creditor aging summaries listing all customers with outstanding credit balances and all suppliers with pending accounts payable.

**Why this priority**: Managing accounts receivable and payable is crucial for working capital and cash collection.

**Independent Test**: Create customer debts and supplier payables, then query balance reports to verify lists of debtors/creditors and total receivables/payables.

**Acceptance Scenarios**:
1. **Given** 3 customers with outstanding balances of $100, $250, and $0, **When** querying customer balance report, **Then** only customers with non-zero balances are listed with total receivables of $350.
2. **Given** 2 suppliers with payable balances of $500 and $1,200, **When** querying supplier balance report, **Then** total payables is $1,700.

---

### User Story 5 - Cash Register Flow & Reconciliation Audit (Priority: P2)

Store Owners need a financial audit trail of the store's daily cash drawer, including opening floats, cash sales inflows, expense outflows, supplier cash payouts, customer cash collections, and end-of-day count discrepancies.

**Why this priority**: Cash is vulnerable to shrinkage; detailed cash audit reports ensure transparency and accountability.

**Independent Test**: Perform various cash transactions and EOD closures over several days, then query the cash register report to verify daily opening floats, net cash flows, and total discrepancy losses/surpluses.

**Acceptance Scenarios**:
1. **Given** a day with $500 float, $300 cash sales, $50 cash expense, and a -$10 closing discrepancy, **When** viewing the daily cash audit, **Then** opening float is $500, net operational cash flow is +$250, closing expected is $750, actual counted is $740, and discrepancy is -$10.

---

## Edge Cases

- **Zero Activity Periods**: Querying reports for date ranges with no transactions must return zeroed-out aggregates (0 counts, 0.00 currency values) without throwing null reference exceptions or division-by-zero errors in margin calculations.
- **Negative Margin / Loss**: When COGS or Expenses exceed Revenue, profit percentages must correctly report negative margins.
- **Timezone Alignment**: Date filters (`from`, `to`) must accurately align with store local time / UTC boundaries without dropping transactions occurring at the boundary of a day.
- **Role-Based Access Control**: Cashiers MUST NOT have access to P&L, Supplier Balances, or Store Financial Reports; only Owner and Manager roles are authorized.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a Sales Summary Report endpoint supporting filtering by `from` and `to` date timestamps.
- **FR-002**: System MUST compute Gross Sales, Total Discounts, Total Returns, Net Sales, and Total Orders in the sales report.
- **FR-003**: System MUST provide a Sales by Product and Sales by Category breakdown ranking top-selling items by revenue and quantity.
- **FR-004**: System MUST provide a Sales by Payment Method breakdown (Cash vs Credit vs Mixed).
- **FR-005**: System MUST provide a Profit & Loss (P&L) Report calculating Net Sales, COGS (summed from line item costs recorded at sale time), Gross Profit, Operating Expenses, Net Profit, and Gross/Net Margin percentages.
- **FR-006**: System MUST provide an Inventory Valuation Report calculating total stock value per product and across the store using Weighted Average Cost (WAC).
- **FR-007**: System MUST provide a Stock Movement Ledger endpoint for any specified product showing complete chronological audit history across all transaction reasons.
- **FR-008**: System MUST provide a Low Stock Alert Report listing all products where current stock is at or below reorder threshold.
- **FR-009**: System MUST provide Customer Accounts Receivable & Supplier Accounts Payable balance summary reports.
- **FR-010**: System MUST provide a Cash Register Audit Report detailing daily opening floats, inflows, outflows, and end-of-day discrepancies.
- **FR-011**: System MUST enforce strict multi-tenant isolation so reports only aggregate data belonging to the authenticated user's store.
- **FR-012**: System MUST restrict all financial, P&L, and balance reporting endpoints to `Owner` and `Manager` roles only.
- **FR-013**: System MUST calculate revenue and P&L reports on an Accrual basis by default (including all completed sales matched against COGS), and support an optional query filter `basis=Cash|Accrual` to filter for cash-collected sales only.
- **FR-014**: System MUST support real-time inventory valuation by default, and support an optional `asOfDate` query parameter to compute historical point-in-time valuation and balances reconstructed from the immutable inventory transactions ledger.
- **FR-015**: System MUST deliver reports as structured JSON responses by default, and support CSV tabular export via a `format=csv` query parameter for external accounting analysis.

---

## Key Entities & Data Models

- **Report Aggregation Models (Read-Only / Projections)**:
  - `SalesReportResponse`, `SalesByProductResponse`, `SalesByCategoryResponse`, `SalesByPaymentMethodResponse`
  - `ProfitLossReportResponse`
  - `InventoryValuationReportResponse`, `StockMovementLedgerResponse`, `LowStockReportResponse`
  - `CustomerBalancesReportResponse`, `SupplierBalancesReportResponse`
  - `CashRegisterAuditReportResponse`

---

## Success Criteria *(mandatory)*

- **SC-001**: All report queries execute with response times under 500ms for stores with up to 100,000 historical transactions.
- **SC-002**: Mathematical accuracy of P&L: `Gross Profit == Net Sales - COGS` and `Net Profit == Gross Profit - Expenses` holds across all test scenarios.
- **SC-003**: 100% of integration tests pass verifying all 5 reporting user stories and role-based access restrictions.
- **SC-004**: Multi-tenant isolation is verified with 0% data leakage across stores.

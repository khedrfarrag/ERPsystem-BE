# Quickstart & Integration Validation: Phase 4 — Reporting & Business Analytics

This document details executable end-to-end integration testing scenarios to validate all 5 reporting user stories.

---

## Scenario 1: Sales Analytics & Breakdown Verification
1. Register store, create categories, units, and products.
2. Record opening stock for Product A ($10 cost, $25 price) and Product B ($50 cost, $100 price).
3. Execute Cash Sale of 2 units of Product A ($50) and Credit Sale of 1 unit of Product B ($100).
4. Execute 1 unit return of Product A ($25).
5. Query `GET /api/reports/sales`:
   - Verify `GrossSales == 150.00`, `TotalReturns == 25.00`, `NetSales == 125.00`.
   - Verify payment breakdown shows Cash $25 and Credit $100.
   - Verify top selling product rankings.

---

## Scenario 2: Profit & Loss (P&L) Formula Accuracy
1. From Scenario 1 state (Net Sales = $125):
   - COGS for remaining items: 1 unit of Product A ($10) + 1 unit of Product B ($50) = $60.
   - Gross Profit = $125 - $60 = $65 (52.0% gross margin).
2. Record an operating expense of $20.
3. Query `GET /api/reports/profit-loss`:
   - Verify `NetSalesRevenue == 125.00`.
   - Verify `CostOfGoodsSold == 60.00`.
   - Verify `GrossProfit == 65.00`.
   - Verify `OperatingExpenses == 20.00`.
   - Verify `NetProfit == 45.00` (36.0% net margin).

---

## Scenario 3: Real-Time vs Historical Inventory Valuation
1. Product A has 9 units remaining (at $10 WAC) = $90.
2. Product B has 9 units remaining (at $50 WAC) = $450.
3. Query `GET /api/reports/inventory/valuation`:
   - Verify `TotalValuation == 540.00`.
4. Query with `asOfDate` before the sales occurred:
   - Verify valuation reconstructs the initial opening balance stock value ($100 + $500 = $600).

---

## Scenario 4: Product Stock Movement Ledger Audit
1. Query `GET /api/reports/inventory/movement/{productId}` for Product A:
   - Verify 3 chronological movements: `OpeningBalance (+10)`, `Sale (-2)`, `SaleReturn (+1)`.
   - Verify resulting balance after each line is accurately tracked: `10 -> 8 -> 9`.

---

## Scenario 5: CSV Streaming Export Format
1. Query `GET /api/reports/sales?format=csv`:
   - Verify Content-Type is `text/csv`.
   - Verify header row and values match comma-separated tabular structure.

---

## Scenario 6: Role-Based Access Restriction
1. Authenticate as Cashier:
   - Query `GET /api/reports/profit-loss` -> Verify `403 Forbidden`.
   - Query `GET /api/reports/sales` -> Verify `403 Forbidden`.
   - Query `GET /api/reports/balances/suppliers` -> Verify `403 Forbidden`.

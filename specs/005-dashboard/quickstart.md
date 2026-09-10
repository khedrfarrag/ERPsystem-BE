# Quickstart & Verification Guide: Phase 5 — Dashboard & Real-Time KPIs

## Overview
This guide defines how to manually and automatically verify all Phase 5 Dashboard endpoints.

---

## 1. Prerequisites
- API running on `http://localhost:5030` or integration test harness.
- Store Owner / Manager authentication token.
- Cashier authentication token (to verify 403 Forbidden).

---

## 2. Verification Scenarios

### Scenario 1: Dashboard Summary Accuracy
1. Authenticate as Store Owner.
2. Complete a cash sale (e.g. 500 EGP) and an expense (e.g. 100 EGP).
3. Call `GET /api/dashboard/summary`.
4. **Assert**:
   - `todaySalesRevenue >= 500`
   - `todayExpenses >= 100`
   - `todayGrossProfit` and `todayOperatingProfit` accurately reflect recorded margins.

### Scenario 2: Zero-Gap Sales Trend
1. Call `GET /api/dashboard/sales-trend?days=7`.
2. **Assert**:
   - Response contains exactly 7 daily points.
   - All points have valid `date` strings in ascending order.
   - Non-sale days have `revenue: 0` and `ordersCount: 0`.

### Scenario 3: Inventory Movement (Top & Slow-Moving Products)
1. Call `GET /api/dashboard/top-products?days=30&limit=5`.
2. **Assert**: Returns ranked list ordered by `quantitySold` descending.
3. Call `GET /api/dashboard/slow-moving-products?days=30&limit=10`.
4. **Assert**: Returns active items with `currentStock > 0` and no sales in the last 30 days.

### Scenario 4: Critical Low-Stock Alerts
1. Set a product stock to `1` with `minStockLevel = 5`.
2. Call `GET /api/dashboard/low-stock-alerts`.
3. **Assert**: Item is present with `deficitQuantity = 4`.

### Scenario 5: Role Authorization & Tenant Security
1. Authenticate as Cashier.
2. Call `GET /api/dashboard/summary`.
3. **Assert**: Returns `403 Forbidden`.
4. Authenticate as Store Owner for Store B.
5. **Assert**: Store B dashboard contains 0 records from Store A.

---

## 3. Automated Integration Tests
Run via:
```bash
dotnet test tests/RetailOS.IntegrationTests/RetailOS.IntegrationTests.csproj --filter "FullyQualifiedName~Dashboard"
```

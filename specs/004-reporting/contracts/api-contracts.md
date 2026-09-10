# API Contracts: Phase 4 — Reporting & Business Analytics

All endpoints reside under `/api/reports/` and require JWT authentication with `Owner` or `Manager` roles.

---

## Endpoints

### 1. Sales Summary Report
- **URL**: `GET /api/reports/sales`
- **Roles**: `Owner`, `Manager`
- **Query Parameters**:
  - `from` (`DateTimeOffset?`): Start date filter.
  - `to` (`DateTimeOffset?`): End date filter.
  - `customerId` (`Guid?`): Filter for specific customer.
  - `paymentMethod` (`string?`): Filter for `Cash`, `Credit`, or `Mixed`.
  - `basis` (`string?`): `Accrual` (default) or `Cash`.
  - `format` (`string?`): `json` (default) or `csv`.
- **Response 200 (JSON)**: `ApiResponse<SalesSummaryReportResponse>`
- **Response 200 (CSV)**: `text/csv` attachment `sales-report-{date}.csv`.

---

### 2. Profit & Loss (P&L) Report
- **URL**: `GET /api/reports/profit-loss`
- **Roles**: `Owner`, `Manager`
- **Query Parameters**:
  - `from` (`DateTimeOffset?`)
  - `to` (`DateTimeOffset?`)
  - `basis` (`string?`): `Accrual` (default) or `Cash`.
- **Response 200 (JSON)**: `ApiResponse<ProfitLossReportResponse>`

---

### 3. Inventory Valuation Report
- **URL**: `GET /api/reports/inventory/valuation`
- **Roles**: `Owner`, `Manager`
- **Query Parameters**:
  - `asOfDate` (`DateTimeOffset?`): Point-in-time calculation (default: real-time current stock & WAC).
  - `categoryId` (`Guid?`)
  - `format` (`string?`): `json` (default) or `csv`.
- **Response 200 (JSON)**: `ApiResponse<InventoryValuationReportResponse>`
- **Response 200 (CSV)**: `text/csv` attachment `inventory-valuation-{date}.csv`.

---

### 4. Product Stock Movement Ledger
- **URL**: `GET /api/reports/inventory/movement/{productId}`
- **Roles**: `Owner`, `Manager`
- **Query Parameters**:
  - `from` (`DateTimeOffset?`)
  - `to` (`DateTimeOffset?`)
  - `format` (`string?`): `json` (default) or `csv`.
- **Response 200 (JSON)**: `ApiResponse<ProductStockMovementReportResponse>`

---

### 5. Low Stock Alerts Report
- **URL**: `GET /api/reports/inventory/low-stock`
- **Roles**: `Owner`, `Manager`
- **Query Parameters**:
  - `categoryId` (`Guid?`)
- **Response 200 (JSON)**: `ApiResponse<IReadOnlyList<LowStockAlertItemResponse>>`

---

### 6. Customer Balances (Receivables) Report
- **URL**: `GET /api/reports/balances/customers`
- **Roles**: `Owner`, `Manager`
- **Query Parameters**:
  - `hasBalanceOnly` (`bool`, default: `true`)
  - `format` (`string?`): `json` (default) or `csv`.
- **Response 200 (JSON)**: `ApiResponse<CustomerBalancesReportResponse>`

---

### 7. Supplier Balances (Payables) Report
- **URL**: `GET /api/reports/balances/suppliers`
- **Roles**: `Owner`, `Manager`
- **Query Parameters**:
  - `hasBalanceOnly` (`bool`, default: `true`)
  - `format` (`string?`): `json` (default) or `csv`.
- **Response 200 (JSON)**: `ApiResponse<SupplierBalancesReportResponse>`

---

### 8. Cash Register Flow & Reconciliation Report
- **URL**: `GET /api/reports/cash-register`
- **Roles**: `Owner`, `Manager`
- **Query Parameters**:
  - `from` (`DateTimeOffset?`)
  - `to` (`DateTimeOffset?`)
  - `format` (`string?`): `json` (default) or `csv`.
- **Response 200 (JSON)**: `ApiResponse<CashRegisterAuditReportResponse>`

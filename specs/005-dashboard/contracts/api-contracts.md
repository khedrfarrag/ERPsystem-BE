# API Contracts: Phase 5 — Dashboard & Real-Time KPIs

All endpoints are hosted under `/api/dashboard` and require `Authorization: Bearer <token>` with `Owner` or `Manager` role. Unauthorized roles (`Cashier`, `InventoryClerk`) receive `403 Forbidden`.

---

## Endpoints

### 1. `GET /api/dashboard/summary`
Returns real-time aggregated summary KPI cards for today, month-to-date, current cash drawer, and balances.

#### Response: `200 OK`
```json
{
  "success": true,
  "data": {
    "todaySalesRevenue": 1500.00,
    "todayOrdersCount": 5,
    "todayCashSales": 1000.00,
    "todayCreditSales": 500.00,
    "todayGrossProfit": 450.00,
    "todayExpenses": 150.00,
    "todayOperatingProfit": 300.00,
    "monthToDateSalesRevenue": 45000.00,
    "monthToDateOperatingProfit": 12500.00,
    "isCashRegisterOpen": true,
    "liveCashDrawerBalance": 1200.00,
    "activeRegisterCashierName": "Ahmed Cashier",
    "totalReceivables": 3200.00,
    "totalPayables": 4500.00,
    "lowStockCount": 3,
    "outOfStockCount": 1
  },
  "message": "Dashboard summary retrieved successfully"
}
```

---

### 2. `GET /api/dashboard/sales-trend`
Returns time-series daily sales, revenue, and gross profit data.

#### Query Parameters:
- `days` (optional, integer, default: `7`, e.g. `7` or `30`)
- `startDate` (optional, ISO date, e.g. `2026-09-01`)
- `endDate` (optional, ISO date, e.g. `2026-09-07`)

#### Response: `200 OK`
```json
{
  "success": true,
  "data": {
    "totalDays": 7,
    "totalPeriodRevenue": 12450.00,
    "totalPeriodProfit": 3600.00,
    "totalPeriodOrders": 42,
    "points": [
      {
        "date": "2026-08-31",
        "revenue": 1200.00,
        "grossProfit": 350.00,
        "ordersCount": 4
      },
      {
        "date": "2026-09-01",
        "revenue": 0.00,
        "grossProfit": 0.00,
        "ordersCount": 0
      },
      {
        "date": "2026-09-02",
        "revenue": 2100.00,
        "grossProfit": 620.00,
        "ordersCount": 8
      }
    ]
  },
  "message": "Sales trend retrieved successfully"
}
```

---

### 3. `GET /api/dashboard/top-products`
Returns ranked top-selling products by quantity and revenue over a timeframe.

#### Query Parameters:
- `days` (optional, integer, default: `30`)
- `limit` (optional, integer, default: `5`, max: `20`)

#### Response: `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "productId": "9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
      "productName": "Ariel Gel 2.5L",
      "barcode": "6223000112233",
      "unitName": "Bottle",
      "quantitySold": 85.00,
      "totalRevenue": 14450.00,
      "currentStock": 22.00
    }
  ],
  "message": "Top products retrieved successfully"
}
```

---

### 4. `GET /api/dashboard/slow-moving-products`
Returns active products with positive stock that have zero sales within the evaluation window.

#### Query Parameters:
- `days` (optional, integer, default: `30`)
- `limit` (optional, integer, default: `10`, max: `50`)

#### Response: `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "productId": "e1f1deb4-3b7d-4bad-9bdd-2b0d7b3dcb6e",
      "productName": "Industrial Degreaser 5L",
      "barcode": "6223000998877",
      "unitName": "Canister",
      "currentStock": 15.00,
      "unitCost": 120.00,
      "tiedUpCapital": 1800.00,
      "lastSaleDate": "2026-07-15T10:30:00Z",
      "daysSinceLastSale": 52
    }
  ],
  "message": "Slow-moving products retrieved successfully"
}
```

---

### 5. `GET /api/dashboard/low-stock-alerts`
Returns critical inventory shortages (`CurrentStock <= MinStockLevel`).

#### Query Parameters:
- `limit` (optional, integer, default: `10`, max: `50`)

#### Response: `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "productId": "f2a1deb4-3b7d-4bad-9bdd-2b0d7b3dcb6f",
      "productName": "Pril Dishwashing Liquid 1L",
      "barcode": "6223000445566",
      "unitName": "Bottle",
      "currentStock": 2.00,
      "minStockLevel": 10.00,
      "deficitQuantity": 8.00,
      "isOutOfStock": false
    }
  ],
  "message": "Low-stock alerts retrieved successfully"
}
```

---

### 6. `GET /api/dashboard/recent-activity`
Returns the most recent transactions stream.

#### Query Parameters:
- `limit` (optional, integer, default: `10`, max: `50`)

#### Response: `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "id": "c1f1deb4-3b7d-4bad-9bdd-2b0d7b3dcb7a",
      "activityType": "SALE",
      "referenceNumber": "INV-20260905-0012",
      "description": "Sale to Walk-in Customer",
      "amount": 450.00,
      "timestamp": "2026-09-05T11:45:00Z",
      "performedBy": "Ahmed Cashier"
    },
    {
      "id": "d2f1deb4-3b7d-4bad-9bdd-2b0d7b3dcb7b",
      "activityType": "EXPENSE",
      "referenceNumber": "EXP-20260905-0003",
      "description": "Electricity Bill",
      "amount": 250.00,
      "timestamp": "2026-09-05T10:15:00Z",
      "performedBy": "Store Manager"
    }
  ],
  "message": "Recent activity retrieved successfully"
}
```

# 🚀 RetailOS Backend — Frontend Integration Guide

Welcome to the **RetailOS** Frontend Developer Integration Guide. This document provides everything you need to connect your frontend application (React, Next.js, Vue, Flutter, etc.) to the local backend API.

---

## 1. ⚙️ Local Development Setup & URLs

### Backend Service URLs:
- **HTTP**: `http://localhost:5030`
- **HTTPS**: `https://localhost:7106`
- **Interactive Swagger UI**: [`http://localhost:5030/swagger`](http://localhost:5030/swagger)

### Starting the Backend Locally:
```bash
cd g:\system-analysiss-saas\system-BE
dotnet run --project src/RetailOS.Api
```

### CORS Configuration:
- ✅ CORS is **fully enabled** for all local origins (`localhost:3000`, `localhost:5173`, `localhost:8080`, `localhost:4200`, etc.) with `AllowAnyMethod()`, `AllowAnyHeader()`, and `AllowCredentials()`.

---

## 2. 🧪 Demo Data & Ready-to-use Store (Seeder)

To populate a complete, realistic detergents & retail store with 15+ products, categories, suppliers, customers, 7 days of sales charts, and expenses:

### Trigger Seeder via API:
```http
POST /api/seed/demo-store?force=true
```

### 🔑 Pre-seeded Test Accounts:
| Role | Email | Password | Access Level |
|:---|:---|:---|:---|
| **Owner** | `owner@retailos.com` | `Pass123456!` | Full access (Products, POS, Reports, Dashboard, Settings, Users). |
| **Manager** | `manager@retailos.com` | `Pass123456!` | Operational & Management access (Reports, Dashboard, Purchases, Stock). |
| **Cashier** | `cashier@retailos.com` | `Pass123456!` | Cash Register, Sales/POS, Customers (Management Dashboard/Reports are restricted - 403). |

---

## 3. 🔐 Authentication & Tenant Context

All authenticated endpoints expect the JWT access token in the `Authorization` header:
```http
Authorization: Bearer <accessToken>
```

### Standard API Response Envelope:
All responses follow a uniform JSON structure:
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation successful",
  "errors": []
}
```

---

## 4. 📚 Core Modules & Endpoint Directory

### A. Authentication (`/api/auth`)
- `POST /api/auth/register` — Register new store & owner account.
- `POST /api/auth/login` — Login with email/password (returns `accessToken`, `refreshToken`, `storeId`, `role`).
- `POST /api/auth/refresh` — Refresh expired access token using `refreshToken`.

---

### B. Dashboard & Real-Time KPIs (`/api/dashboard`) *(Owner & Manager only)*
- `GET /api/dashboard/summary` — Overview cards (Today's Sales, Orders Count, Cash/Credit split, Operating Profit, Live Drawer Cash, Receivables, Payables, Low Stock counts).
- `GET /api/dashboard/sales-trend?days=7` — Continuous time-series sales/profit trend chart data (zero-gap date filled).
- `GET /api/dashboard/top-products?days=30&limit=5` — Top-selling products ranking by volume and revenue.
- `GET /api/dashboard/slow-moving-products?days=30&limit=10` — Stagnant products with positive stock and zero sales.
- `GET /api/dashboard/low-stock-alerts` — Critical inventory shortages (`Stock <= MinStockLevel`).
- `GET /api/dashboard/recent-activity?limit=10` — Live activity stream of latest sales, purchases, and expenses.

---

### C. Catalog & Inventory (`/api/products`, `/api/categories`, `/api/units`, `/api/inventory`)
- `GET /api/categories` & `POST /api/categories` — Product categories.
- `GET /api/units` & `POST /api/units` — Measurement units (قطعة، لتر، كرتونة، إلخ).
- `GET /api/products` — Paginated products with search & category filters.
- `POST /api/products` — Create new product with barcode, costs, and `minStockLevel`.
- `GET /api/products/{id}` & `PUT /api/products/{id}` — Update product details.
- `POST /api/inventory/opening-stock` — Record initial inventory balance.
- `GET /api/inventory/stock/{productId}` — Get real-time stock and movement transactions.

---

### D. POS & Sales Operations (`/api/sales`)
- `POST /api/sales` — Checkout sale (`PaymentMethod`: `"Cash"`, `"Credit"`, or `"Mixed"`).
- `GET /api/sales` — Filterable sales invoice list (by date range, customer, payment method).
- `GET /api/sales/{id}` — Get full invoice details with line items.
- `POST /api/sales/{id}/returns` — Process full or partial sale return.

---

### E. Cash Register Management (`/api/cash-register`)
- `GET /api/cash-register/current` — Live register balance, inflows, and outflows.
- `POST /api/cash-register/open` — Open cash drawer with opening float (`{ "amount": 500, "notes": "Morning float" }`).
- `POST /api/cash-register/close` — End of day reconciliation (`{ "countedAmount": 1250, "notes": "EOD count" }`).

---

### F. Customers & Suppliers (`/api/customers`, `/api/suppliers`)
- `GET /api/customers` & `POST /api/customers` — Manage retail and wholesale customers.
- `GET /api/customers/{id}/ledger` — Customer balance audit statement.
- `GET /api/suppliers` & `POST /api/suppliers` — Manage vendors and distributors.
- `GET /api/suppliers/{id}/ledger` — Supplier balance audit statement.

---

### G. Purchases & Expenses (`/api/purchases`, `/api/expenses`)
- `POST /api/purchases` — Record supplier purchases with Weighted Average Cost (WAC) updates.
- `GET /api/expenses/categories` & `POST /api/expenses/categories` — Expense categories.
- `GET /api/expenses` & `POST /api/expenses` — Record daily operational expenses.

---

### H. Reports & Analytics (`/api/reports`)
- `GET /api/reports/sales?format=json|csv` — Sales summary & breakdown report.
- `GET /api/reports/profit-loss` — Full Profit & Loss (Revenue - COGS = Gross Profit, Gross Profit - Expenses = Net Operating Profit).
- `GET /api/reports/inventory/valuation` — Inventory valuation (real-time & historical `asOfDate`).
- `GET /api/reports/balances/customers` — Accounts receivable aging.
- `GET /api/reports/balances/suppliers` — Accounts payable aging.
- `GET /api/reports/cash-register/audit` — Daily cash flow audit.

---

## 5. 💡 Next Steps for Frontend Development
1. Start the API locally: `dotnet run --project src/RetailOS.Api`
2. Open Swagger at [`http://localhost:5030/swagger`](http://localhost:5030/swagger)
3. Call `POST /api/seed/demo-store?force=true` to populate data.
4. Login with `owner@retailos.com` / `Pass123456!` to get your JWT token.
5. Happy coding! 🚀

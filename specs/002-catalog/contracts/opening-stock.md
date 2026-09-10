# API Contract: Opening Stock

**Base URL**: `/api/inventory/opening-stock`
**Authentication**: JWT Bearer
**Required Role**: Owner only
**Tenant Scope**: All operations automatically scoped to the authenticated user's store

---

## Overview

Opening stock is recorded once per product when the store goes live. Each entry creates an
`InventoryTransaction` with reason `OPENING_BALANCE`, seeding the inventory ledger and the
Weighted Average Cost (WAC) baseline for that product.

---

## Endpoints

### POST /api/inventory/opening-stock
Record opening stock for a single product.

**Request Body**:
```json
{
  "productId": "uuid",
  "quantity": 150.0,
  "costPerUnit": 5.00
}
```

**Validation**:
- `productId`: required, must exist and be active in this store
- `quantity`: required, > 0
- `costPerUnit`: required, >= 0 (free goods are allowed at zero cost)

**Business Rules**:
- A product can only have one OPENING_BALANCE transaction. A second request for the same
  product returns 409.
- The transaction is written with `created_by` = the authenticated user's ID.

**Response 201**:
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "productId": "uuid",
    "productName": "Nile Water 1.5L",
    "quantity": 150.0,
    "costPerUnit": 5.00,
    "totalValue": 750.00,
    "reason": "OPENING_BALANCE",
    "createdAt": "2026-09-01T10:00:00Z"
  },
  "message": "Opening stock recorded successfully."
}
```

**Response 400**: Validation errors.
**Response 404**: Product not found in this store.
**Response 409**: `{ "success": false, "code": "OPENING_STOCK_ALREADY_EXISTS", "message": "Opening stock has already been recorded for this product." }`

---

### POST /api/inventory/opening-stock/bulk
Record opening stock for multiple products in one request. Convenience endpoint for store setup.

**Request Body**:
```json
{
  "entries": [
    { "productId": "uuid-1", "quantity": 150.0, "costPerUnit": 5.00 },
    { "productId": "uuid-2", "quantity": 48.0, "costPerUnit": 12.50 },
    { "productId": "uuid-3", "quantity": 200.0, "costPerUnit": 0.75 }
  ]
}
```

**Validation**:
- `entries`: required, at least 1 item, max 500 items per request.
- Each entry follows the same rules as the single endpoint.

**Business Rules**:
- Each valid entry is processed individually (not all-or-nothing).
- Products that already have OPENING_BALANCE are reported as skipped — not a request failure.
- Products not found in the store are reported per entry.

**Response 200**:
```json
{
  "success": true,
  "data": {
    "totalRequested": 3,
    "created": 2,
    "skippedAlreadyExists": 1,
    "skippedInvalid": 0,
    "errors": [
      {
        "productId": "uuid-2",
        "code": "OPENING_STOCK_ALREADY_EXISTS",
        "message": "Opening stock has already been recorded for this product."
      }
    ]
  }
}
```

---

### GET /api/inventory/opening-stock
List all opening stock entries for this store.

**Query Parameters**:
| Param | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Page number |
| `pageSize` | int | 25 | Items per page (max 100) |

**Response 200**: Paginated list of opening stock transaction records.

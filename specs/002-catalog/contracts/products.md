# API Contract: Products

**Base URL**: `/api/products`
**Authentication**: JWT Bearer (all endpoints)
**Tenant Scope**: All operations automatically scoped to the authenticated user's store

---

## Roles

| Endpoint | Required Role |
|---|---|
| GET (list, get by id, barcode lookup) | Any authenticated role |
| POST / PUT / PATCH status | Owner, Manager |
| DELETE (soft) | Owner only |
| POST /import/preview | Owner |
| POST /import/commit | Owner |

---

## Product Object (Response Schema)

```json
{
  "id": "uuid",
  "name": "Nile Water 1.5L",
  "barcode": "6223000123456",
  "description": "Nile natural water bottle",
  "sellingPrice": 7.50,
  "purchaseCost": 5.00,
  "minStockLevel": 24.0,
  "imageUrl": null,
  "isActive": true,
  "category": {
    "id": "uuid",
    "name": "Beverages"
  },
  "unit": {
    "id": "uuid",
    "name": "Piece",
    "symbol": "pcs"
  },
  "currentStock": 150.0,
  "createdAt": "2026-09-01T00:00:00Z",
  "updatedAt": "2026-09-01T00:00:00Z"
}
```

> **Note**: `currentStock` is derived from `SUM(quantity)` in `inventory_transactions` for this product. It is computed and included in the response for convenience.

---

## Endpoints

### GET /api/products
List products with search, filtering, and pagination.

**Query Parameters**:
| Param | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Page number |
| `pageSize` | int | 25 | Items per page (max 100) |
| `search` | string? | null | Search by name or barcode (contains) |
| `categoryId` | uuid? | null | Filter by category |
| `isActive` | bool? | null | Filter by active status |
| `inStock` | bool? | null | `true` = only in-stock (currentStock > 0); `false` = only out-of-stock; omitted = all |

**Response 200**: Paginated list with `items`, `totalCount`, `page`, `pageSize`.

---

### GET /api/products/{id}
Get a product by ID.

**Response 200**: Full product object.
**Response 404**: `{ "success": false, "code": "PRODUCT_NOT_FOUND" }`

---

### GET /api/products/barcode/{barcode}
Look up a product by barcode. Used by POS sales flow.

**Response 200**: Full product object.
**Response 404**: `{ "success": false, "code": "PRODUCT_NOT_FOUND", "message": "No product found with this barcode." }`

---

### POST /api/products
Create a new product manually.

**Request Body**:
```json
{
  "name": "Nile Water 1.5L",
  "categoryId": "uuid",
  "unitId": "uuid",
  "sellingPrice": 7.50,
  "barcode": "6223000123456",
  "description": "Nile natural water bottle",
  "purchaseCost": 5.00,
  "minStockLevel": 24.0
}
```

**Validation**:
- `name`: required, max 300 chars, unique in store (case-insensitive)
- `categoryId`: required, must exist and be active in this store
- `unitId`: required, must exist and be active in this store
- `sellingPrice`: required, > 0
- `barcode`: optional, max 100 chars, unique in store when provided
- `purchaseCost`: optional, >= 0
- `minStockLevel`: optional, >= 0

**Response 201**: Created product object.
**Response 400**: Validation errors.
**Response 404**: Category or Unit not found.
**Response 409**: Name or barcode conflict.

---

### PUT /api/products/{id}
Update a product's details.

**Request Body**: Same schema as POST (all fields provided).

**Response 200**: Updated product object.
**Response 400 / 404 / 409**: Validation / not found / conflict.

---

### PATCH /api/products/{id}/status
Activate or deactivate a product.

**Request Body**: `{ "isActive": false }`
**Response 200**: Updated product object.

---

### DELETE /api/products/{id}
Soft-delete a product.

**Business Rule**: If product has any inventory transactions, returns 409.

**Response 204**: Deleted.
**Response 409**: `{ "success": false, "code": "PRODUCT_HAS_TRANSACTIONS" }`

---

## Bulk Import Endpoints

### POST /api/products/import/preview
Parse and validate an import file. **No database writes.**

**Request**: `multipart/form-data`
- `file`: CSV or XLSX file, max 5MB

**Response 200**:
```json
{
  "success": true,
  "data": {
    "totalRows": 520,
    "validRows": 498,
    "errorRows": 22,
    "errors": [
      {
        "row": 5,
        "field": "sellingPrice",
        "message": "Selling price must be greater than zero."
      },
      {
        "row": 12,
        "field": "name",
        "message": "Product name cannot be empty."
      }
    ]
  }
}
```

**Response 400**: File format unsupported, file exceeds 5MB, or zero valid rows.

---

### POST /api/products/import/commit
Commit the valid rows from an import file into the database.

**Request**: `multipart/form-data`
- `file`: Same file as preview (re-uploaded); max 5MB

**Business Rules**:
- Invalid rows are silently skipped (reported in result).
- Products with duplicate name OR barcode in the store are reported as skipped-duplicate.
- All valid, non-duplicate rows are inserted in a single transaction.

**Response 200**:
```json
{
  "success": true,
  "data": {
    "totalProcessed": 520,
    "created": 490,
    "skippedDuplicate": 8,
    "skippedInvalid": 22
  }
}
```

**Response 400**: File invalid or zero processable rows.

---

## Expected Import File Format

### CSV Column Order (fixed)

```
name, barcode, category_name, unit_symbol, selling_price, purchase_cost, min_stock_level, description
```

- `category_name`: must match an existing active category name in the store (case-insensitive).
- `unit_symbol`: must match an existing active unit symbol in the store (case-insensitive).
- `barcode`, `purchase_cost`, `min_stock_level`, `description`: optional (leave blank).

### XLSX Format
Same columns in the same order, row 1 is the header, data starts at row 2.

# Quickstart Validation Guide: Phase 2 — Catalog

**Purpose**: End-to-end validation scenarios that prove the Catalog module works correctly.
**Prerequisites**: Phase 1 completed, `retailos_dev` PostgreSQL database migrated.

---

## Prerequisites

1. Phase 1 tests pass: `dotnet test` → 21/21 green.
2. Migration `AddCatalogAndInventoryLedger` applied:
   ```powershell
   dotnet ef database update --project src/RetailOS.Infrastructure --startup-project src/RetailOS.Api
   ```
3. A registered store with Owner credentials (run `/api/auth/register` to get tokens).

---

## Scenario 1: Category Lifecycle

**Verify**: Create → List → Update → Deactivate → Attempt reuse → Delete (when empty)

```http
# 1. Create category
POST /api/categories
Authorization: Bearer <owner_token>
{ "name": "Beverages", "description": "All drinks" }
→ 201, id = cat_id

# 2. List categories
GET /api/categories
→ 200, items contains "Beverages", isActive=true

# 3. Attempt duplicate name
POST /api/categories
{ "name": "BEVERAGES" }
→ 409, code=CATEGORY_NAME_CONFLICT

# 4. Deactivate
PATCH /api/categories/{cat_id}/status
{ "isActive": false }
→ 200, isActive=false

# 5. Soft-delete (no products linked)
DELETE /api/categories/{cat_id}
→ 204

# 6. Verify gone from list
GET /api/categories
→ 200, "Beverages" no longer in items
```

---

## Scenario 2: Unit Creation

```http
# Create unit
POST /api/units
{ "name": "Piece", "symbol": "pcs" }
→ 201, id = unit_id

# Duplicate symbol attempt
POST /api/units
{ "name": "Single Piece", "symbol": "PCS" }
→ 409, code=UNIT_SYMBOL_CONFLICT
```

---

## Scenario 3: Product Manual Creation

**Verify**: All validations, unique constraints, and barcode lookup.

```http
# Create category and unit first (or reuse from Scenario 1/2)
POST /api/categories { "name": "Dairy" } → 201, dairy_cat_id
POST /api/units { "name": "Piece", "symbol": "pcs" } → 201, pcs_unit_id

# Create product
POST /api/products
{
  "name": "Milk 1L",
  "categoryId": dairy_cat_id,
  "unitId": pcs_unit_id,
  "sellingPrice": 25.00,
  "barcode": "6223001234567",
  "purchaseCost": 18.00,
  "minStockLevel": 10
}
→ 201, id = prod_id, currentStock = 0

# Duplicate name (case-insensitive)
POST /api/products { "name": "MILK 1L", ... }
→ 409, code=PRODUCT_NAME_CONFLICT

# Barcode lookup
GET /api/products/barcode/6223001234567
→ 200, name="Milk 1L"

# Invalid price
POST /api/products { ..., "sellingPrice": 0 }
→ 400, validation error on sellingPrice

# Deactivate (can still be looked up, blocked in sales)
PATCH /api/products/{prod_id}/status { "isActive": false }
→ 200, isActive=false
```

---

## Scenario 4: Bulk Import — Full Workflow

**Verify**: Preview → Error feedback → Commit → Result summary → Duplicate detection.

```http
# Prepare file: valid_products.csv with 10 rows, 2 intentionally invalid

# Step 1: Preview (no writes)
POST /api/products/import/preview
Content-Type: multipart/form-data
file=@valid_products.csv
→ 200, totalRows=10, validRows=8, errorRows=2

# Step 2: Commit (with same file)
POST /api/products/import/commit
file=@valid_products.csv
→ 200, totalProcessed=10, created=8, skippedInvalid=2, skippedDuplicate=0

# Step 3: Re-commit same file (all 8 now duplicates)
POST /api/products/import/commit
file=@valid_products.csv
→ 200, created=0, skippedDuplicate=8, skippedInvalid=2
```

---

## Scenario 5: Opening Stock

**Verify**: One-time constraint, ledger creation, bulk entry.

```http
# Single opening stock
POST /api/inventory/opening-stock
{ "productId": prod_id, "quantity": 100.0, "costPerUnit": 18.00 }
→ 201, totalValue=1800.00

# Verify stock reflected
GET /api/products/{prod_id}
→ 200, currentStock=100.0

# Second attempt (same product) → rejected
POST /api/inventory/opening-stock
{ "productId": prod_id, "quantity": 50.0, "costPerUnit": 17.00 }
→ 409, code=OPENING_STOCK_ALREADY_EXISTS

# Bulk opening stock (multiple products at once)
POST /api/inventory/opening-stock/bulk
{ "entries": [ { "productId": "uuid-A", "quantity": 48, "costPerUnit": 12.5 }, ... ] }
→ 200, created=N, skippedAlreadyExists=0
```

---

## Scenario 6: Tenant Isolation

**Verify**: Store A's products are invisible to Store B.

```http
# Register two stores
POST /api/auth/register { "storeName": "Store A", ..., "email": "a@test.com" } → tokens_a
POST /api/auth/register { "storeName": "Store B", ..., "email": "b@test.com" } → tokens_b

# Create product in Store A
POST /api/products (Bearer tokens_a.AccessToken)
{ "name": "Store A Product", ... } → 201, prod_a_id

# Store B tries to read Store A's product
GET /api/products/{prod_a_id} (Bearer tokens_b.AccessToken)
→ 404 (not visible, not a 403 — product simply doesn't exist in Store B's scope)

# Store B's product list
GET /api/products (Bearer tokens_b.AccessToken)
→ 200, items=[] (Store B has no products)
```

---

## Automated Test Coverage Targets

| Scenario | Test Class |
|---|---|
| Category CRUD + soft-delete guard | `CategoryCrudTests` |
| Unit CRUD + symbol uniqueness | `UnitCrudTests` |
| Product CRUD + barcode lookup | `ProductCrudTests` |
| Product validation rules | `ProductValidationTests` |
| Bulk import preview + commit | `BulkImportTests` |
| Opening stock single + bulk | `OpeningStockTests` |
| Opening stock uniqueness guard | `OpeningStockTests` |
| Catalog tenant isolation | `CatalogTenantIsolationTests` |
| Deactivated entity usage guard | `EntityLifecycleTests` |

All tests use the real PostgreSQL instance (local fallback in `TestWebApplicationFactory`).

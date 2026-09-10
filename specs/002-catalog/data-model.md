# Data Model: Phase 2 — Catalog

**Feature**: Product Catalog (Categories, Units, Products, Bulk Import, Opening Stock)
**Constitution**: v1.1.0 — snake_case, numeric(19,4), soft-delete, tenant-scoped

---

## Base Classes

### SoftDeletableEntity (NEW — extends BaseEntity)

Introduced in this phase for entities with historical audit requirements.

```
SoftDeletableEntity : BaseEntity
├── DeletedAt          : DateTime?     -- nullable; null = not deleted
└── IsDeleted          : bool (computed, not mapped) → DeletedAt.HasValue
```

All three new entity types (`Category`, `Unit`, `Product`) extend `SoftDeletableEntity`.
`BaseEntity` from Phase 1 already provides: `Id (Guid PK)`, `CreatedAt`, `UpdatedAt`.

---

## Entities

### Category

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | `uuid` | PK | |
| `store_id` | `uuid` | FK → stores(id), NOT NULL, INDEX | Tenant scope |
| `name` | `varchar(200)` | NOT NULL | Case-insensitive unique per store (enforced via functional index on `lower(name)`) |
| `description` | `text` | nullable | |
| `is_active` | `boolean` | NOT NULL, DEFAULT true | Inactive = cannot assign to new products |
| `created_at` | `timestamptz` | NOT NULL | |
| `updated_at` | `timestamptz` | NOT NULL | |
| `deleted_at` | `timestamptz` | nullable | Soft-delete; global query filter excludes non-null rows |

**Indexes**:
```sql
CREATE UNIQUE INDEX ix_categories_store_name
  ON categories (store_id, lower(name))
  WHERE deleted_at IS NULL;

CREATE INDEX ix_categories_store_id ON categories (store_id);
```

**EF Core configuration**:
- `HasQueryFilter(c => c.DeletedAt == null)` — global, automatic, inherited from `SoftDeletableEntity`
- `HasIndex(c => c.StoreId)`
- Functional unique index via `migrationBuilder.Sql(...)` (EF Core cannot express `lower()` natively)

---

### Unit

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | `uuid` | PK | |
| `store_id` | `uuid` | FK → stores(id), NOT NULL, INDEX | Tenant scope |
| `name` | `varchar(100)` | NOT NULL | Case-insensitive unique per store |
| `symbol` | `varchar(20)` | NOT NULL | Unique per store (e.g., "kg", "pcs", "L") |
| `description` | `text` | nullable | |
| `is_active` | `boolean` | NOT NULL, DEFAULT true | |
| `created_at` | `timestamptz` | NOT NULL | |
| `updated_at` | `timestamptz` | NOT NULL | |
| `deleted_at` | `timestamptz` | nullable | |

**Indexes**:
```sql
CREATE UNIQUE INDEX ix_units_store_name
  ON units (store_id, lower(name))
  WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX ix_units_store_symbol
  ON units (store_id, lower(symbol))
  WHERE deleted_at IS NULL;

CREATE INDEX ix_units_store_id ON units (store_id);
```

---

### Product

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | `uuid` | PK | |
| `store_id` | `uuid` | FK → stores(id), NOT NULL, INDEX | Tenant scope |
| `category_id` | `uuid` | FK → categories(id), NOT NULL | Must reference active category |
| `unit_id` | `uuid` | FK → units(id), NOT NULL | Must reference active unit |
| `name` | `varchar(300)` | NOT NULL | Case-insensitive unique per store |
| `barcode` | `varchar(100)` | nullable | Unique per store when NOT NULL |
| `description` | `text` | nullable | |
| `selling_price` | `numeric(19,4)` | NOT NULL, > 0 | Enforced at application layer |
| `purchase_cost` | `numeric(19,4)` | nullable, >= 0 | Optional; seeds WAC for Opening Stock |
| `min_stock_level` | `numeric(19,4)` | nullable, >= 0 | Low-stock alert threshold |
| `image_url` | `varchar(500)` | nullable | Deferred feature; placeholder preserved |
| `is_active` | `boolean` | NOT NULL, DEFAULT true | |
| `created_at` | `timestamptz` | NOT NULL | |
| `updated_at` | `timestamptz` | NOT NULL | |
| `deleted_at` | `timestamptz` | nullable | |

**Indexes**:
```sql
CREATE UNIQUE INDEX ix_products_store_name
  ON products (store_id, lower(name))
  WHERE deleted_at IS NULL;

CREATE UNIQUE INDEX ix_products_store_barcode
  ON products (store_id, barcode)
  WHERE barcode IS NOT NULL AND deleted_at IS NULL;

CREATE INDEX ix_products_store_id ON products (store_id);
CREATE INDEX ix_products_store_category ON products (store_id, category_id);
CREATE INDEX ix_products_store_active ON products (store_id, is_active);
```

---

### InventoryTransaction

> **Note**: This is the Phase 2 introduction of the inventory ledger. Phase 3 will append rows to this table using additional `reason` values — no structural changes will be needed.

| Column | Type | Constraints | Notes |
|---|---|---|---|
| `id` | `uuid` | PK | |
| `store_id` | `uuid` | FK → stores(id), NOT NULL, INDEX | Tenant scope |
| `product_id` | `uuid` | FK → products(id), NOT NULL | Must exist and belong to same store |
| `quantity` | `numeric(19,4)` | NOT NULL | Signed: positive = inflow, negative = outflow |
| `cost_per_unit` | `numeric(19,6)` | NOT NULL, >= 0 | Unit cost at transaction time (6dp for WAC precision) |
| `reason` | `varchar(30)` | NOT NULL | Enum: see InventoryTransactionReason below |
| `reference_id` | `uuid` | nullable | FK to source document (purchase_id, sale_id, etc.) — null for OPENING_BALANCE |
| `notes` | `text` | nullable | Optional operator notes |
| `created_by` | `uuid` | FK → users(id), NOT NULL | Audit — who made the entry |
| `created_at` | `timestamptz` | NOT NULL | Immutable; no updated_at (ledger is append-only) |

**Indexes**:
```sql
-- Primary query: current stock per product
CREATE INDEX ix_inv_tx_store_product ON inventory_transactions (store_id, product_id);

-- One OPENING_BALANCE per product per store (uniqueness enforced by partial index)
CREATE UNIQUE INDEX ix_inv_tx_opening_balance
  ON inventory_transactions (store_id, product_id)
  WHERE reason = 'opening_balance';

-- Reporting: time-ordered ledger per store
CREATE INDEX ix_inv_tx_store_created ON inventory_transactions (store_id, created_at);
```

**InventoryTransactionReason Enum** (C# domain enum → `varchar(30)` stored as string):

```
OPENING_BALANCE   — Initial stock entry (Phase 2)
PURCHASE          — Goods received from supplier (Phase 3)
SALE              — Goods sold to customer (Phase 3)
PURCHASE_RETURN   — Returned to supplier (Phase 3)
SALE_RETURN       — Returned by customer (Phase 3)
DAMAGE            — Damaged stock write-off (Phase 3)
LOSS              — Shrinkage/loss write-off (Phase 3)
ADJUSTMENT        — Manual stock correction (Phase 3)
TRANSFER          — Inter-location transfer (Future)
```

**Important**: In Phase 2, only `OPENING_BALANCE` rows are created. The other reasons are defined in the enum now so Phase 3 has no migration impact on the enum type.

---

## Relationships Diagram

```
stores (1) ──< categories (N)    [store_id]
stores (1) ──< units (N)         [store_id]
stores (1) ──< products (N)      [store_id]
stores (1) ──< inventory_transactions (N) [store_id]

categories (1) ──< products (N)  [category_id]
units (1) ──< products (N)       [unit_id]
products (1) ──< inventory_transactions (N) [product_id]
users (1) ──< inventory_transactions (N) [created_by]
```

---

## Migration

**Migration name**: `AddCatalogAndInventoryLedger`

**Actions**:
1. Create `categories` table
2. Create `units` table
3. Create `products` table (FK to categories, units, stores)
4. Create `inventory_transactions` table (FK to products, stores, users)
5. Add functional unique indexes via `migrationBuilder.Sql(...)` (5 total — see indexes above)
6. Add partial unique index for opening_balance deduplication

All existing Phase 1 tables (`stores`, `users`, `refresh_tokens`) are untouched.

---

## WAC Seeding Logic (Opening Stock)

When an `OPENING_BALANCE` transaction is created:

```
current_quantity  = SUM(quantity) FROM inventory_transactions WHERE store_id = X AND product_id = Y
current_cost_sum  = SUM(quantity * cost_per_unit) FROM inventory_transactions WHERE store_id = X AND product_id = Y

# Since OPENING_BALANCE is the first and only transaction at this point:
initial_wac = opening_cost_per_unit  (directly, no division needed)
```

Phase 3 purchase service will recalculate WAC as:
```
new_wac = (existing_value + new_purchase_value) / (existing_qty + new_qty)
```

The Phase 2 opening stock cost correctly seeds this formula without any special handling.

# Research Notes: Phase 2 — Catalog

**Feature**: Product Catalog (Categories, Units, Products, Bulk Import, Opening Stock)
**Date**: 2026-09-01
**Constitution**: v1.1.0

---

## Decision 1: CSV / Excel Parsing Library

**Decision**: Use **CsvHelper** (for CSV) and **ClosedXML** (for XLSX).

**Rationale**:
- Both are mature, widely-used .NET packages with no native dependencies.
- CsvHelper handles CSV edge cases (quoted fields, BOM, varied line endings) correctly.
- ClosedXML reads `.xlsx` without requiring Office/COM interop; works on Linux/Windows equally.
- Both are well-tested and maintained; no custom parsing logic is needed.
- ExcelDataReader was considered but is lower-level and requires more boilerplate.
- EPPlus was considered but its license is commercial for non-personal use.

**Packages**:
```
CsvHelper >= 33.0
ClosedXML >= 0.104
```

**Alternatives considered**:
- ExcelDataReader: lower-level, more code needed, no advantage
- EPPlus: commercial license (GPL fallback requires commercial for production)
- SpreadsheetLight: less maintained, smaller community

---

## Decision 2: Bulk Import — Two-Phase Workflow (Preview → Commit)

**Decision**: Stateless two-call API. No server-side session state.

**Approach**:
1. `POST /api/products/import/preview` — accepts multipart file, returns validation summary JSON (valid rows, error rows). **No database writes**.
2. `POST /api/products/import/commit` — accepts multipart file again (or re-parsed from re-upload). Writes valid rows atomically.

**Rationale**:
- Stateless design fits the Simple Modular Monolith constraint perfectly (no Redis, no session store, no temp file storage service).
- Re-uploading the file for commit is standard practice in this class of system (Excel import pattern used by ERPs). File sizes are bounded (5MB max).
- Avoids a temporary import-session table and the cleanup logic that comes with it.
- The preview response is sufficient for the user to review and decide.

**Alternatives considered**:
- Server-side temp storage of parsed rows: requires cleanup jobs, session IDs, TTL — violates KISS.
- Single-call import with immediate write: no preview, bad UX for large imports.

---

## Decision 3: InventoryTransaction Ledger Design

**Decision**: `InventoryTransaction` is an append-only ledger table introduced in Phase 2 and extended (not modified) in Phase 3.

**Schema pillars**:
- `quantity`: signed decimal — positive for inflows (PURCHASE, OPENING_BALANCE), negative for outflows (SALE, DAMAGE, LOSS).
- `cost_per_unit`: the unit cost at time of transaction (`numeric(19,6)` for WAC precision).
- `reason`: enum — `OPENING_BALANCE | PURCHASE | SALE | PURCHASE_RETURN | SALE_RETURN | DAMAGE | LOSS | ADJUSTMENT | TRANSFER`.
- `reference_id`: nullable Guid — links to the originating document (purchase_id, sale_id) when created by Phase 3+ modules.
- `created_by`: Guid (UserId) — audit trail.

**Current stock** for a product = `SUM(quantity)` WHERE `store_id = @storeId AND product_id = @productId`.

**Opening Balance seed**: One `InventoryTransaction` row with `reason = OPENING_BALANCE`, quantity = entered quantity, cost_per_unit = entered cost. This seeds the WAC formula used by Phase 3.

**Constitution compliance**: Ledger pattern at aggregate level (not event sourcing). ✅

---

## Decision 4: Concurrency on Opening Stock Insert

**Decision**: Optimistic concurrency via **unique constraint** — not row-level locking.

**Rationale**:
- Opening stock is a one-time-per-product operation. The constraint "one OPENING_BALANCE per product per store" is enforced by a **partial unique index** on `inventory_transactions(store_id, product_id)` WHERE `reason = 'opening_balance'`.
- A second insert attempt will throw a `UniqueConstraintViolation` caught by the application layer and returned as a domain error `OPENING_STOCK_ALREADY_EXISTS`.
- This is simpler and more correct than optimistic concurrency tokens for an insert-once operation.
- Full optimistic concurrency (RowVersion) will be added to a future `ProductStockSummary` view or materialized balance when Phase 3 introduces concurrent sale/purchase mutations.

---

## Decision 5: Soft-Delete Pattern for Category / Unit / Product

**Decision**: `deleted_at` nullable timestamp column on each entity; EF Core `HasQueryFilter` filters `WHERE deleted_at IS NULL`.

**Implementation**:
- `BaseEntity` (already exists from Phase 1) has `CreatedAt`, `UpdatedAt`. Add `DeletedAt` (nullable) to the soft-deletable variant.
- Introduce `SoftDeletableEntity : BaseEntity` with `DeletedAt` + `IsDeleted` property.
- Products, Categories, and Units extend `SoftDeletableEntity`.
- The global query filter on these types adds `&& !e.IsDeleted` automatically.
- Hard-delete attempts where linked records exist: caught by FK constraint (PostgreSQL) OR by explicit check in the service layer before calling `SaveChanges`.

**Constitution compliance**: "any entity that has historical records linked to it MUST NOT be hard-deleted" ✅

---

## Decision 6: Partial Unique Index for Nullable Barcode

**Decision**: PostgreSQL partial unique index on `products(store_id, barcode) WHERE barcode IS NOT NULL`.

**Rationale**:
- A standard unique index on a nullable column allows multiple NULLs in standard SQL, but EF Core/Npgsql behavior requires explicit configuration.
- The cleanest approach: `HasIndex(p => new { p.StoreId, p.Barcode }).IsUnique().HasFilter("barcode IS NOT NULL")` in EF Core Fluent API.
- This enforces: two products in the same store cannot share a barcode, but many products can have no barcode (`null`).

---

## Decision 7: Case-Insensitive Name Uniqueness

**Decision**: PostgreSQL `CITEXT` extension OR functional index on `LOWER(name)`.

**Chosen**: **Functional index on `LOWER(name)`** — avoids adding the `citext` extension (simpler, no DDL privilege requirement on some hosted Postgres services).

**EF Core configuration**:
```csharp
HasIndex(p => new { p.StoreId })
    .HasDatabaseName("ix_products_store_id_name_lower")
    .HasFilter(null)
// Enforced via: raw SQL in migration: CREATE UNIQUE INDEX ... ON products (store_id, lower(name)) WHERE deleted_at IS NULL
```

Migration will include `migrationBuilder.Sql(...)` for the functional index since EF Core Fluent API does not natively support `LOWER()` index expressions as of EF Core 9.

---

## Decision 8: Bulk Import — Row-Level Atomicity

**Decision**: Each valid row is inserted in a **single transaction** wrapping all valid rows. If the transaction fails (e.g., constraint race), the entire import is rolled back.

**Rationale**:
- "Atomic at the row level" from the spec means: invalid/duplicate rows are **skipped** (not retried), valid rows are **all-or-nothing** within one DB transaction.
- This is simpler and safer than inserting row-by-row with individual try/catch blocks.
- A race condition where two concurrent imports create the same product name will result in one import succeeding and the other failing with a clear constraint error — the user is informed to retry.

---

## Summary Table

| # | Decision | Choice |
|---|---|---|
| 1 | CSV/XLSX parsing | CsvHelper + ClosedXML |
| 2 | Import preview/commit | Stateless two-call API (re-upload for commit) |
| 3 | Inventory ledger design | Append-only signed-quantity ledger table |
| 4 | Opening stock concurrency | Partial unique index (DB constraint) |
| 5 | Soft-delete pattern | `SoftDeletableEntity` base + `deleted_at` + global query filter |
| 6 | Nullable barcode uniqueness | Partial unique index `WHERE barcode IS NOT NULL` |
| 7 | Case-insensitive name uniqueness | Functional index on `LOWER(name)` via raw SQL migration |
| 8 | Import atomicity | Single transaction for all valid rows |

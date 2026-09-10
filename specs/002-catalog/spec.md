# Feature Specification: Phase 2 — Catalog

**Feature Directory**: `specs/002-catalog`
**Feature**: Product Catalog (Categories, Units, Products, Opening Stock)
**Status**: Draft
**Version**: 1.0.0
**Created**: 2026-09-01
**Constitution Compliance**: v1.1.0

---

## Overview

Store owners and authorized staff need a structured catalog to define the products their store
sells before any sales, purchases, or inventory operations can take place. This feature enables
staff to organize products into categories and measurement units, create product records either
individually or in bulk, and initialize opening stock balances so the system reflects the store's
actual inventory from day one.

The catalog is the foundation of all downstream business operations: a product cannot be sold,
purchased, or tracked without existing as a catalog entry first.

---

## Clarifications

### Session 2026-09-01

- Q: When a product was created with an active category, and that category is later deactivated, what should happen to existing products that reference it? → A: Existing products are **unaffected** — deactivating a category is a forward-guard only. It blocks new products from being assigned to the deactivated category/unit, but does not change the active/inactive status of products already using it.
- Q: During bulk import, if a row references a category name or unit symbol that does not exist in the store yet, what should the system do with that row? → A: Row is **skipped with a clear per-row error** (e.g., "Category 'Beverages' does not exist. Please create it first."). Auto-creation of missing categories/units is not supported. Users must pre-create all categories and units before importing.
- Q: When listing products via GET /api/products, should products with zero current stock be shown by default? → A: **All products are shown by default** (zero-stock included). An optional `inStock` boolean query parameter is supported — when `inStock=true` only products with `currentStock > 0` are returned; when `inStock=false` only out-of-stock products are returned. This supports both sales (POS) and admin (catalog management) views without changing the default behavior.

---

## Business Context

A typical retail store migrating to RetailOS has an existing product list — sometimes hundreds
or thousands of items — stored in spreadsheets or another system. Entering these one by one is
impractical. Additionally, products need to be organized by category (e.g., Beverages, Electronics,
Dairy) and measured in appropriate units (pieces, kilograms, liters, boxes). Initial stock counts
and purchase costs must be recorded so the inventory ledger starts in a known, accurate state.

---

## Actors

| Actor | Description |
|---|---|
| **Owner** | Full control over catalog: create, edit, activate/deactivate, bulk import, opening stock |
| **Manager** | Can create and edit products; cannot hard-delete or adjust opening stock |
| **InventoryClerk** | Can view products and categories; cannot create or modify |
| **Cashier** | Can browse the product catalog for use in sales; read-only access |

---

## User Stories

### US1 — Owner/Manager Organizes Product Taxonomy (Priority: P1)

> **As a** store Owner or Manager,
> **I want to** create and manage categories and units of measurement,
> **So that** products can be properly classified and compared.

**Acceptance Criteria**:
- A category has a name (required), optional description, and an active/inactive flag.
- A unit has a name (required), short symbol (required, e.g., "kg", "pcs"), optional description, and an active/inactive flag.
- Category and unit names must be unique within a store.
- Inactive categories/units remain visible in historical records but cannot be assigned to new products. **Existing products already referencing a deactivated category or unit are not affected — their active status and category/unit assignment remain unchanged.**
- A category or unit that has products linked to it cannot be hard-deleted — it can only be deactivated.

---

### US2 — Owner/Manager Creates Products Manually (Priority: P1)

> **As a** store Owner or Manager,
> **I want to** create product records one at a time with all relevant attributes,
> **So that** the catalog reflects the real products the store carries.

**Acceptance Criteria**:
- A product record requires at minimum: name, category, unit of measurement, selling price.
- Optional fields: barcode (must be unique within store when provided), description, purchase cost,
  minimum stock level (low-stock alert threshold), and an image reference.
- Product names must be unique within a store (case-insensitive).
- Barcodes are optional; when provided they must be unique within the store.
- Products can be marked active or inactive. Inactive products cannot be sold or purchased.
- A product linked to any transaction (sale, purchase, inventory record) cannot be hard-deleted — only deactivated.
- Selling price must be greater than zero.
- Purchase cost, if provided, must be greater than or equal to zero.

---

### US3 — Owner Imports Product List in Bulk (Priority: P1)

> **As a** store Owner,
> **I want to** upload a file containing my entire product list,
> **So that** I can migrate my existing catalog without entering each product manually.

**Acceptance Criteria**:

**Upload & Parse**:
- Accepted file format: CSV and Excel (.xlsx) files, maximum 5MB.
- The system parses the file and extracts product rows.

**Validate**:
- Every row is validated against the same rules as manual product creation.
- Validation errors are reported per row (row number + specific error).
- If a row references a `category_name` or `unit_symbol` that does not exist in the store, the row is **skipped with a clear error** (e.g., `"Category 'Dairy' does not exist. Please create it first."`). Auto-creation of missing categories or units is not supported.
- A file with zero valid rows is rejected entirely.

**Preview**:
- The user can see a structured preview: total rows, valid rows, error rows, and a paginated error list.
- The user can correct the file and re-upload.

**Confirm & Commit**:
- The user explicitly confirms to proceed with import (valid rows only).
- The import is atomic at the row level: each valid row is committed; invalid rows are skipped and reported.
- Duplicate detection: a product with the same name OR barcode already in the store is treated as a conflict (reported, not overwritten).

**Result Summary**:
- After import, the user receives: total processed, successfully created, skipped duplicates, skipped invalid.

**Access**: Only Owners can perform bulk imports.

---

### US4 — Owner Records Opening Stock (Priority: P1)

> **As a** store Owner,
> **I want to** record the initial quantity and cost of each product when going live,
> **So that** the inventory ledger starts with an accurate baseline.

**Acceptance Criteria**:
- An opening stock entry requires: product, quantity (> 0), and purchase cost per unit at the time of entry.
- Each opening stock entry creates an inventory transaction with reason `OPENING_BALANCE`.
- Opening stock can only be recorded once per product. A second attempt for the same product must be rejected with a clear error.
- Opening stock entries are made at the store level (per tenant).
- Inventory totals after opening stock must be derivable from the ledger transaction history.
- The operation is restricted to Owners.

---

## Functional Requirements

### Categories

| ID | Requirement |
|---|---|
| FR-CAT-01 | A category must have a unique name within the store (case-insensitive match). |
| FR-CAT-02 | A category can be activated or deactivated; deactivated categories cannot be assigned to new products. |
| FR-CAT-03 | A category with linked products cannot be hard-deleted; it can only be deactivated. |
| FR-CAT-04 | Category listing must support pagination and optional filtering by active status. |
| FR-CAT-05 | All category operations are scoped to the authenticated user's store (tenant isolation). |

### Units of Measurement

| ID | Requirement |
|---|---|
| FR-UNIT-01 | A unit must have a unique name and unique symbol within the store. |
| FR-UNIT-02 | A unit can be activated or deactivated; deactivated units cannot be assigned to new products. |
| FR-UNIT-03 | A unit with linked products cannot be hard-deleted; it can only be deactivated. |
| FR-UNIT-04 | Unit listing must support pagination and optional filtering by active status. |
| FR-UNIT-05 | All unit operations are scoped to the authenticated user's store (tenant isolation). |

### Products

| ID | Requirement |
|---|---|
| FR-PRD-01 | A product must have a unique name within the store (case-insensitive). |
| FR-PRD-02 | A product barcode, when provided, must be unique within the store. |
| FR-PRD-03 | Selling price must be a positive decimal value. |
| FR-PRD-04 | Purchase cost, when provided, must be a non-negative decimal value. |
| FR-PRD-05 | Products linked to any transaction cannot be hard-deleted; they must be deactivated. |
| FR-PRD-06 | Product listing must support pagination, search by name or barcode, and filtering by category, active status, and optionally by stock availability (`inStock=true` returns only products with current stock > 0; `inStock=false` returns only out-of-stock products; omitted = all products). |
| FR-PRD-07 | All product operations are scoped to the authenticated user's store (tenant isolation). |
| FR-PRD-08 | Product lookup by barcode must be supported for use in POS workflows. |

### Bulk Import

| ID | Requirement |
|---|---|
| FR-IMP-01 | Supported file formats: CSV and Excel (.xlsx), up to 5MB. |
| FR-IMP-02 | Import validation must use identical rules to manual product creation. |
| FR-IMP-03 | Validation errors must be reported per row with row number and message. |
| FR-IMP-04 | A preview response must be returned before the user commits. |
| FR-IMP-05 | User must explicitly confirm import before any records are written to the database. |
| FR-IMP-06 | Products with duplicate name or barcode within the store are reported and skipped, not overwritten. |
| FR-IMP-07 | Import result must include: total, created, skipped-duplicate, skipped-invalid counts. |
| FR-IMP-08 | Only Owners can initiate a bulk import. |

### Opening Stock

| ID | Requirement |
|---|---|
| FR-OST-01 | An opening stock entry requires: product ID, quantity (> 0), purchase cost per unit (>= 0). |
| FR-OST-02 | A product can have at most one opening stock entry; a second attempt is rejected. |
| FR-OST-03 | Every opening stock entry must create an inventory transaction record with reason `OPENING_BALANCE`. |
| FR-OST-04 | Opening stock operations are scoped to the authenticated store and restricted to the Owner role. |
| FR-OST-05 | Bulk opening stock entry (multiple products in one request) must be supported for convenience during initial setup. |

---

## Success Criteria

1. **A store Owner can organize and populate a full product catalog without developer assistance**, including assigning categories and units, adding barcodes, and setting prices.
2. **A store with 500 products can be fully imported in under 5 minutes** via the bulk import workflow, including validation feedback and confirmation.
3. **Validation errors are communicated at the row level** so the user can correct specific problems without re-uploading the entire file.
4. **Opening stock entries produce a verifiable inventory baseline**: the quantity and value derivable from transaction history must match what the owner entered.
5. **No catalog record belonging to Store A can be read or written by Store B**, verified by automated tests.
6. **Inactive products, categories, and units remain visible in historical data** but are blocked from use in new transactions.
7. **Duplicate products are detected and reported** during import — no silent overwrites occur.

---

## Scope Boundaries

### In Scope
- Categories CRUD (Owner/Manager write, all roles read)
- Units of Measurement CRUD (Owner/Manager write, all roles read)
- Products CRUD (Owner/Manager write, all roles read)
- Product search by name and barcode lookup
- Bulk product import (CSV/XLSX) with preview–confirm–commit workflow
- Opening stock recording per product (single and bulk)

### Out of Scope (deferred)
- Product images (placeholder field preserved in model, upload deferred)
- Product variants (e.g., color/size) — single SKU per product in this phase
- Price tiers or customer-specific pricing
- Promotional pricing / discount rules
- Multi-location / multi-branch stock
- ETA electronic invoice integration (see Constitution Out of Scope)
- Supplier product codes (deferred to Suppliers module, Phase 3)

---

## Key Entities

| Entity | Key Attributes |
|---|---|
| `Category` | id, store_id, name, description, is_active, created_at, updated_at, deleted_at |
| `Unit` | id, store_id, name, symbol, description, is_active, created_at, updated_at, deleted_at |
| `Product` | id, store_id, category_id, unit_id, name, barcode, description, selling_price, purchase_cost, min_stock_level, is_active, image_url (nullable), created_at, updated_at, deleted_at |
| `InventoryTransaction` | id, store_id, product_id, quantity, cost_per_unit, reason (enum), reference_id (nullable), notes, created_by, created_at |

---

## Dependencies

- **Phase 1 — Foundation**: Store, User, Role, Tenant isolation, Auth, and the `AppDbContext` with
  `HasQueryFilter` and `TenantSaveChangesInterceptor` must be in place and passing all tests.
- **Phase 3 — Business Operations**: Products must exist before Purchases, Sales, or Inventory
  adjustments can reference them. The `InventoryTransaction` table introduced here is the same
  ledger that Phase 3 operations will append to.

---

## Assumptions

1. Products in this phase are single-SKU (no variants). This is a known simplification; the model
   will preserve enough flexibility to add variants later without requiring a rewrite.
2. The barcode field is a free-form string (EAN-13, QR, internal code) — format validation is not
   enforced in this phase.
3. Opening stock is a one-time operation per product at store launch. Adjustments after go-live
   will be handled via Inventory Adjustment transactions in Phase 3.
4. Bulk import file column order is fixed and documented in the import template; dynamic column
   detection is deferred.
5. Weighted Average Cost (WAC) calculation will be triggered by Phase 3 purchase operations.
   The opening stock cost per unit seeds the initial WAC.
6. The `image_url` field is a nullable string preserved in the model. File upload is deferred.

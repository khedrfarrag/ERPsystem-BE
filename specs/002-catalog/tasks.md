# Tasks: Phase 2 — Catalog

**Feature**: Product Catalog (Categories, Units, Products, Bulk Import, Opening Stock)
**Feature Directory**: `specs/002-catalog`
**Constitution**: v1.1.0
**Generated**: 2026-09-01

> **Convention**: `[P]` = parallelizable task (independent files, no incomplete-task dependencies). `[USN]` = maps to User Story N from spec.md.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Install new NuGet packages and create the `SoftDeletableEntity` base class — required before any catalog entity can be created.

- [X] T001 Add `CsvHelper` and `ClosedXML` NuGet packages to `src/RetailOS.Infrastructure/RetailOS.Infrastructure.csproj`
- [X] T002 Create `SoftDeletableEntity` base class extending `BaseEntity` with `DeletedAt (DateTime?)` in `src/RetailOS.Domain/Common/SoftDeletableEntity.cs`
- [X] T003 Extend `AppDbContext.OnModelCreating` to apply `HasQueryFilter(e => e.DeletedAt == null)` for all `SoftDeletableEntity` subtypes using a model loop in `src/RetailOS.Infrastructure/Persistence/AppDbContext.cs`

**Checkpoint**: `dotnet build` passes. `SoftDeletableEntity` exists and is registered in context filtering.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define all domain entities and enums, create the EF Core migration, and generate the database schema before any user story implementation begins.

- [X] T004 [P] Create `InventoryTransactionReason` enum with all values (`OPENING_BALANCE`, `PURCHASE`, `SALE`, `PURCHASE_RETURN`, `SALE_RETURN`, `DAMAGE`, `LOSS`, `ADJUSTMENT`, `TRANSFER`) in `src/RetailOS.Domain/Enums/InventoryTransactionReason.cs`
- [X] T005 [P] Create `Category` entity extending `SoftDeletableEntity, ITenantEntity` with `StoreId`, `Name`, `Description`, `IsActive` in `src/RetailOS.Domain/Entities/Category.cs`
- [X] T006 [P] Create `Unit` entity extending `SoftDeletableEntity, ITenantEntity` with `StoreId`, `Name`, `Symbol`, `Description`, `IsActive` in `src/RetailOS.Domain/Entities/Unit.cs`
- [X] T007 Create `Product` entity extending `SoftDeletableEntity, ITenantEntity` with all fields (`StoreId`, `CategoryId`, `UnitId`, `Name`, `Barcode`, `Description`, `SellingPrice`, `PurchaseCost`, `MinStockLevel`, `ImageUrl`, `IsActive`) and navigation properties in `src/RetailOS.Domain/Entities/Product.cs`
- [X] T008 Create `InventoryTransaction` entity implementing `ITenantEntity` (no soft-delete — append-only ledger) with `StoreId`, `ProductId`, `Quantity (numeric 19,4)`, `CostPerUnit (numeric 19,6)`, `Reason`, `ReferenceId`, `Notes`, `CreatedBy`, `CreatedAt` in `src/RetailOS.Domain/Entities/InventoryTransaction.cs`
- [X] T009 [P] Create `CategoryConfiguration` EF Fluent API config with `HasIndex(store_id)`, HasPrecision, MaxLength, and `HasQueryFilter` note in `src/RetailOS.Infrastructure/Persistence/Configurations/CategoryConfiguration.cs`
- [X] T010 [P] Create `UnitConfiguration` EF Fluent API config with indexes, MaxLength for Name/Symbol in `src/RetailOS.Infrastructure/Persistence/Configurations/UnitConfiguration.cs`
- [X] T011 Create `ProductConfiguration` EF Fluent API config with `HasPrecision(19,4)` on prices, FK relationships to Category/Unit, MaxLength on Name/Barcode/ImageUrl in `src/RetailOS.Infrastructure/Persistence/Configurations/ProductConfiguration.cs`
- [X] T012 Create `InventoryTransactionConfiguration` EF Fluent API config with `HasPrecision(19,4)` on Quantity, `HasPrecision(19,6)` on CostPerUnit, FK to Product/User/Store, string conversion for Reason enum in `src/RetailOS.Infrastructure/Persistence/Configurations/InventoryTransactionConfiguration.cs`
- [X] T013 Add `DbSet<Category>`, `DbSet<Unit>`, `DbSet<Product>`, `DbSet<InventoryTransaction>` to `AppDbContext` and register all new configurations in `src/RetailOS.Infrastructure/Persistence/AppDbContext.cs`
- [X] T014 Generate EF Core migration `AddCatalogAndInventoryLedger` via `dotnet ef migrations add AddCatalogAndInventoryLedger --project src/RetailOS.Infrastructure --startup-project src/RetailOS.Api`
- [X] T015 Add all functional unique indexes (`lower(name)`, partial barcode, opening_balance partial) as `migrationBuilder.Sql(...)` calls inside the migration in `src/RetailOS.Infrastructure/Persistence/Migrations/<timestamp>_AddCatalogAndInventoryLedger.cs`
- [X] T016 Apply migration to `retailos_dev`: `dotnet ef database update --project src/RetailOS.Infrastructure --startup-project src/RetailOS.Api`

**Checkpoint**: `dotnet build` passes. Database has 4 new tables with all indexes. `dotnet test` still shows 21/21 green (existing Phase 1 tests unaffected).

---

## Phase 3: User Story 1 — Owner/Manager Organizes Product Taxonomy (Priority: P1)

**Goal**: Enable Owners and Managers to create, list, update, deactivate, and soft-delete Categories and Units. Deactivation is a forward-guard only — existing products using a deactivated category/unit are unaffected.

**Independent Test**: Create a category, list it, attempt a duplicate (case-insensitive), deactivate it, verify a new product cannot use it, soft-delete it, verify it disappears from the list. Repeat for Unit.

### Tests for User Story 1

- [X] T017 [P] [US1] Integration tests for Category CRUD: create, list, get-by-id, update, duplicate-name (case-insensitive) rejection, deactivate, soft-delete with/without linked products in `tests/RetailOS.IntegrationTests/Catalog/CategoryCrudTests.cs`
- [X] T018 [P] [US1] Integration tests for Unit CRUD: create, list, duplicate name/symbol rejection, deactivate, soft-delete guard in `tests/RetailOS.IntegrationTests/Catalog/UnitCrudTests.cs`

### Implementation for User Story 1

- [X] T019 [P] [US1] Create Category DTOs (`CreateCategoryRequest`, `UpdateCategoryRequest`, `UpdateCategoryStatusRequest`, `CategoryResponse`, `CategoryListResponse`) in `src/RetailOS.Application/Categories/DTOs/CategoryDTOs.cs`
- [X] T020 [P] [US1] Create Unit DTOs (`CreateUnitRequest`, `UpdateUnitRequest`, `UpdateUnitStatusRequest`, `UnitResponse`, `UnitListResponse`) in `src/RetailOS.Application/Units/DTOs/UnitDTOs.cs`
- [X] T021 [P] [US1] Define `ICategoryService` interface in `src/RetailOS.Application/Categories/Interfaces/ICategoryService.cs`
- [X] T022 [P] [US1] Define `IUnitService` interface in `src/RetailOS.Application/Units/Interfaces/IUnitService.cs`
- [X] T023 [P] [US1] Create FluentValidation `CreateCategoryRequestValidator` and `UpdateCategoryRequestValidator` in `src/RetailOS.Application/Categories/Validators/`
- [X] T024 [P] [US1] Create FluentValidation `CreateUnitRequestValidator` and `UpdateUnitRequestValidator` in `src/RetailOS.Application/Units/Validators/`
- [X] T025 [US1] Implement `CategoryService` with list (paginated, isActive filter, search), create (check name uniqueness via lower()), get-by-id, update, patch-status, soft-delete (guard: reject if has linked products) in `src/RetailOS.Infrastructure/Catalog/CategoryService.cs`
- [X] T026 [US1] Implement `UnitService` with list, create (check name + symbol uniqueness), get-by-id, update, patch-status, soft-delete (guard: reject if has linked products) in `src/RetailOS.Infrastructure/Catalog/UnitService.cs`
- [X] T027 [US1] Implement `CategoriesController` with `GET /api/categories`, `GET /api/categories/{id}`, `POST /api/categories`, `PUT /api/categories/{id}`, `PATCH /api/categories/{id}/status`, `DELETE /api/categories/{id}` with Owner/Manager role enforcement in `src/RetailOS.Api/Controllers/CategoriesController.cs`
- [X] T028 [US1] Implement `UnitsController` with `GET /api/units`, `GET /api/units/{id}`, `POST /api/units`, `PUT /api/units/{id}`, `PATCH /api/units/{id}/status`, `DELETE /api/units/{id}` in `src/RetailOS.Api/Controllers/UnitsController.cs`
- [X] T029 [US1] Register `ICategoryService → CategoryService` and `IUnitService → UnitService` in `src/RetailOS.Infrastructure/DependencyInjection.cs`

**Checkpoint**: Category and Unit CRUD fully functional. T017 and T018 tests pass.

---

## Phase 4: User Story 2 — Owner/Manager Creates Products Manually (Priority: P1)

**Goal**: Enable Owners and Managers to create, view, update, deactivate, and soft-delete product records. Products must be found by barcode (for POS). Product list supports `inStock` optional filter (all shown by default).

**Independent Test**: Create a category and unit, create a product, retrieve by ID and barcode, attempt a duplicate name (case-insensitive) and duplicate barcode, deactivate, verify soft-delete blocked if transactions exist.

### Tests for User Story 2

- [X] T030 [P] [US2] Integration tests for Product CRUD, barcode lookup, name/barcode uniqueness, deactivation, and soft-delete guard in `tests/RetailOS.IntegrationTests/Catalog/ProductCrudTests.cs`
- [X] T031 [P] [US2] Integration tests for product validation rules (zero price, negative cost, inactive category/unit rejection on create) in `tests/RetailOS.IntegrationTests/Catalog/ProductValidationTests.cs`

### Implementation for User Story 2

- [X] T032 [P] [US2] Create Product DTOs (`CreateProductRequest`, `UpdateProductRequest`, `UpdateProductStatusRequest`, `ProductResponse`, `ProductListResponse`) in `src/RetailOS.Application/Products/DTOs/ProductDTOs.cs`
- [X] T033 [P] [US2] Define `IProductService` interface in `src/RetailOS.Application/Products/Interfaces/IProductService.cs`
- [X] T034 [P] [US2] Create `CreateProductRequestValidator` and `UpdateProductRequestValidator` (sellingPrice > 0, purchaseCost >= 0, category/unit must be active) in `src/RetailOS.Application/Products/Validators/`
- [X] T035 [US2] Implement `ProductService` with: list (paginated, search by name/barcode, filter by category/isActive/inStock), create (name uniqueness lower(), barcode uniqueness, active category/unit check), get-by-id, get-by-barcode, update, patch-status, soft-delete (guard: blocked if any inventory_transactions exist) in `src/RetailOS.Infrastructure/Catalog/ProductService.cs`
- [X] T036 [US2] Implement `ProductsController` with `GET /api/products`, `GET /api/products/{id}`, `GET /api/products/barcode/{barcode}`, `POST /api/products`, `PUT /api/products/{id}`, `PATCH /api/products/{id}/status`, `DELETE /api/products/{id}` — all roles read, Owner/Manager write in `src/RetailOS.Api/Controllers/ProductsController.cs`
- [X] T037 [US2] Register `IProductService → ProductService` in `src/RetailOS.Infrastructure/DependencyInjection.cs`

**Checkpoint**: Full product lifecycle works. T030 and T031 tests pass. Barcode lookup works for future POS use.

---

## Phase 5: User Story 3 — Owner Imports Product List in Bulk (Priority: P1)

**Goal**: Owner uploads a CSV or XLSX file (≤5MB). System parses, validates (same rules as manual create), returns a preview. Owner confirms; valid rows are committed in a single transaction. Missing category/unit = per-row error (no auto-create). Duplicates reported and skipped.

**Independent Test**: Upload CSV with 10 rows (2 invalid, 1 with non-existent category). Preview shows 7 valid / 3 error. Commit creates 7 products. Re-commit shows 0 created, 7 skipped-duplicate.

### Tests for User Story 3

- [X] T038 [P] [US3] Integration tests for bulk import: CSV preview, XLSX preview, commit (valid rows), commit idempotency (all duplicates on 2nd run), missing-category per-row error, zero-valid-rows rejection in `tests/RetailOS.IntegrationTests/Catalog/BulkImportTests.cs`

### Implementation for User Story 3

- [X] T039 [P] [US3] Create import DTOs (`ImportPreviewResponse`, `ImportRowError`, `ImportCommitResponse`) in `src/RetailOS.Application/Products/DTOs/ImportDTOs.cs`
- [X] T040 [P] [US3] Define `IProductImportService` interface in `src/RetailOS.Application/Products/Interfaces/IProductImportService.cs`
- [X] T041 [US3] Implement `ProductImportService`: parse CSV (CsvHelper) and XLSX (ClosedXML), validate each row using same `CreateProductRequestValidator` logic (including category/unit lookup — error if not found), deduplicate against store's existing products (name OR barcode match = DUPLICATE), preview mode (no DB writes), commit mode (single transaction for all valid rows) in `src/RetailOS.Infrastructure/Catalog/ProductImportService.cs`
- [X] T042 [US3] Add `POST /api/products/import/preview` and `POST /api/products/import/commit` endpoints to `ProductsController` with `[RequestSizeLimit(5_242_880)]`, `[Authorize(Roles = Roles.Owner)]`, `[Consumes("multipart/form-data")]` in `src/RetailOS.Api/Controllers/ProductsController.cs`
- [X] T043 [US3] Register `IProductImportService → ProductImportService` in `src/RetailOS.Infrastructure/DependencyInjection.cs`

**Checkpoint**: Full import workflow works end-to-end. T038 tests pass.

---

## Phase 6: User Story 4 — Owner Records Opening Stock (Priority: P1)

**Goal**: Owner records initial quantity and cost-per-unit per product, creating an `InventoryTransaction` with reason `OPENING_BALANCE`. One entry per product enforced by partial unique index. Bulk convenience endpoint supported for store setup.

**Independent Test**: Record opening stock for a product, verify `currentStock` becomes the entered quantity, attempt a second entry (same product) → 409 `OPENING_STOCK_ALREADY_EXISTS`. Use bulk endpoint to set opening stock for 3 products at once.

### Tests for User Story 4

- [X] T044 [P] [US4] Integration tests for opening stock: single record, verify inventory ledger row created, second attempt rejected (409), bulk entry success, bulk partial (one already exists), list opening stock entries in `tests/RetailOS.IntegrationTests/Catalog/OpeningStockTests.cs`

### Implementation for User Story 4

- [X] T045 [P] [US4] Create opening stock DTOs (`RecordOpeningStockRequest`, `BulkOpeningStockRequest`, `OpeningStockEntryRequest`, `OpeningStockResponse`, `BulkOpeningStockResponse`) in `src/RetailOS.Application/Inventory/DTOs/OpeningStockDTOs.cs`
- [X] T046 [P] [US4] Define `IInventoryService` interface in `src/RetailOS.Application/Inventory/Interfaces/IInventoryService.cs`
- [X] T047 [P] [US4] Create `RecordOpeningStockRequestValidator` (productId required, quantity > 0, costPerUnit >= 0) in `src/RetailOS.Application/Inventory/Validators/RecordOpeningStockRequestValidator.cs`
- [X] T048 [US4] Implement `InventoryService.RecordOpeningStockAsync`: validate product belongs to store and is active, insert `InventoryTransaction` with reason `OPENING_BALANCE`, catch `UniqueConstraintViolation` from partial index and return `OPENING_STOCK_ALREADY_EXISTS` domain error in `src/RetailOS.Infrastructure/Inventory/InventoryService.cs`
- [X] T049 [US4] Implement `InventoryService.BulkRecordOpeningStockAsync`: process each entry individually (not all-or-nothing), report per-entry success/already-exists/not-found in `src/RetailOS.Infrastructure/Inventory/InventoryService.cs`
- [X] T050 [US4] Implement `InventoryController` with `POST /api/inventory/opening-stock`, `POST /api/inventory/opening-stock/bulk`, `GET /api/inventory/opening-stock` — Owner only for POST, any authenticated role for GET in `src/RetailOS.Api/Controllers/InventoryController.cs`
- [X] T051 [US4] Register `IInventoryService → InventoryService` in `src/RetailOS.Infrastructure/DependencyInjection.cs`

**Checkpoint**: Opening stock records correctly. `GET /api/products/{id}` shows `currentStock` reflecting the ledger. T044 tests pass.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Tenant isolation verification, entity lifecycle guard tests, and final validation.

- [X] T052 [P] Integration tests verifying Store A's categories, units, products, and inventory transactions are invisible to Store B in `tests/RetailOS.IntegrationTests/Catalog/CatalogTenantIsolationTests.cs`
- [X] T053 [P] Integration tests verifying: inactive category cannot be used on new product create (400), inactive unit cannot be used on new product create (400), product with transactions cannot be soft-deleted (409) in `tests/RetailOS.IntegrationTests/Catalog/EntityLifecycleTests.cs`
- [X] T054 Run `dotnet test` — confirm all tests pass (21 Phase 1 + new Phase 2 catalog tests)
- [X] T055 Verify all Quickstart scenarios from `specs/002-catalog/quickstart.md` pass end-to-end
- [X] T056 Verify Module Completion Gate against constitution Definition of Done in `specs/002-catalog/checklists/requirements.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1. **BLOCKS all user stories** — all 4 entities and the migration must exist before any service can be written.
- **Phase 3 (US1 — Taxonomy)**: Depends on Phase 2 only. No dependency on US2/US3/US4.
- **Phase 4 (US2 — Products)**: Depends on Phase 2 + Phase 3 (product requires category/unit to exist in DB). **US3 and US4 depend on this phase.**
- **Phase 5 (US3 — Bulk Import)**: Depends on Phase 4 (`ProductService` validation logic reused).
- **Phase 6 (US4 — Opening Stock)**: Depends on Phase 4 (product must exist). Independent of Phase 5.
- **Phase 7 (Polish)**: Depends on all prior phases.

### Parallel Opportunities Within Phases

- **Phase 2**: T004, T005, T006 can run in parallel. T009, T010 can run in parallel. T011, T012 can run in parallel after T007/T008.
- **Phase 3**: T017, T018, T019, T020, T021, T022, T023, T024 can all run in parallel.
- **Phase 4**: T030, T031, T032, T033, T034 can all run in parallel.
- **Phase 5**: T038, T039, T040 can run in parallel.
- **Phase 6**: T044, T045, T046, T047 can run in parallel.
- **Phase 7**: T052, T053 can run in parallel.

---

## Implementation Strategy

### MVP First (US1 + US2 Only)
1. Complete **Phase 1** (Setup).
2. Complete **Phase 2** (All entities + migration).
3. Complete **Phase 3** (Categories and Units CRUD).
4. Complete **Phase 4** (Products CRUD + barcode lookup).
5. **VALIDATE**: Products can be created, listed, found by barcode. Tenant isolation holds.

### Incremental Delivery
- **Increment 1**: US1 (Taxonomy) → Owners can organize categories and units.
- **Increment 2**: US2 (Products) → Owners can manage individual product records.
- **Increment 3**: US3 (Bulk Import) → Owners can import 500+ products from a file.
- **Increment 4**: US4 (Opening Stock) → Inventory ledger initialized with accurate baselines.
- **Increment 5**: Polish → Tenant isolation + lifecycle guards verified. Phase 2 ready for Phase 3 (Business Operations).

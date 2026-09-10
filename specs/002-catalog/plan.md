# Implementation Plan: Phase 2 — Catalog

**Feature**: Product Catalog (Categories, Units, Products, Bulk Import, Opening Stock)
**Feature Directory**: `specs/002-catalog`
**Branch**: `002-catalog`
**Constitution**: v1.1.0
**Date**: 2026-09-01

---

## Constitution Check

| Constraint | Status | Evidence |
|---|---|---|
| Correctness | ✅ | Inventory ledger is append-only; WAC seeded from opening cost; decimal `numeric(19,4)` enforced |
| Security | ✅ | All endpoints require JWT; role checks per endpoint; tenant from JWT claims only |
| Tenant Isolation | ✅ | `ITenantEntity` + `HasQueryFilter` + `TenantSaveChangesInterceptor` — all 4 entities inherit this |
| KISS / No Premature Abstraction | ✅ | No CQRS, no mediator, no generic repositories; simple Service → DbContext pattern |
| Strong Typing | ✅ | All requests/responses are typed DTOs; no dynamic types; enum for TransactionReason |
| Soft-Delete | ✅ | `SoftDeletableEntity` with `deleted_at`; hard-delete blocked when linked records exist |
| Decimal Precision | ✅ | `numeric(19,4)` for prices; `numeric(19,6)` for `cost_per_unit` in inventory ledger |
| Shared Validation (Import = Manual) | ✅ | Both paths use the same `ProductValidator` — no separate rule sets |
| Async + CancellationToken | ✅ | All DB/IO operations async; CT threaded through |
| AsNoTracking on reads | ✅ | All list and single-item read queries use `AsNoTracking()` |
| No N+1 queries | ✅ | `currentStock` derived via SQL `SUM()` projection, not lazy-loaded navigation |

---

## Technical Context

- **Existing infrastructure reused**: `AppDbContext`, `IStoreContext`, `TenantSaveChangesInterceptor`, `BaseEntity`, `ApiResponse<T>`, `Result<T>`, `ExceptionHandlingMiddleware`, all auth/role middleware.
- **New base class**: `SoftDeletableEntity : BaseEntity` — adds `DeletedAt` nullable field + `HasQueryFilter` extension.
- **New NuGet packages**:
  - `CsvHelper` — CSV parsing
  - `ClosedXML` — XLSX parsing
- **Migration name**: `AddCatalogAndInventoryLedger`

---

## Implementation Phases

### Setup Phase (Prerequisites)

1. Install NuGet packages (`CsvHelper`, `ClosedXML`) in `RetailOS.Infrastructure`.
2. Create `SoftDeletableEntity` base class in `RetailOS.Domain/Common/`.
3. Extend `AppDbContext.OnModelCreating` to apply soft-delete global query filter for all `SoftDeletableEntity` subtypes.

---

### Phase A: Categories & Units (US1)

**Domain** (`RetailOS.Domain/Entities/`):
- `Category.cs` : `SoftDeletableEntity, ITenantEntity`
- `Unit.cs` : `SoftDeletableEntity, ITenantEntity`

**Application** (`RetailOS.Application/`):
- `Categories/DTOs/CategoryDTOs.cs` — `CreateCategoryRequest`, `UpdateCategoryRequest`, `UpdateCategoryStatusRequest`, `CategoryResponse`, `CategoryListResponse`
- `Categories/Interfaces/ICategoryService.cs`
- `Categories/Validators/CreateCategoryRequestValidator.cs`, `UpdateCategoryRequestValidator.cs`
- `Units/DTOs/UnitDTOs.cs` — `CreateUnitRequest`, `UpdateUnitRequest`, `UpdateUnitStatusRequest`, `UnitResponse`, `UnitListResponse`
- `Units/Interfaces/IUnitService.cs`
- `Units/Validators/CreateUnitRequestValidator.cs`, `UpdateUnitRequestValidator.cs`

**Infrastructure** (`RetailOS.Infrastructure/`):
- `Catalog/CategoryService.cs` — implements `ICategoryService`
- `Catalog/UnitService.cs` — implements `IUnitService`
- `Persistence/Configurations/CategoryConfiguration.cs` — HasIndex, functional index notes
- `Persistence/Configurations/UnitConfiguration.cs`

**API** (`RetailOS.Api/Controllers/`):
- `CategoriesController.cs` — full CRUD + status patch + soft-delete
- `UnitsController.cs` — full CRUD + status patch + soft-delete

**Tests**:
- `tests/RetailOS.IntegrationTests/Catalog/CategoryCrudTests.cs`
- `tests/RetailOS.IntegrationTests/Catalog/UnitCrudTests.cs`

---

### Phase B: Products (US2)

**Domain** (`RetailOS.Domain/`):
- `Entities/Product.cs` : `SoftDeletableEntity, ITenantEntity`
- `Enums/InventoryTransactionReason.cs` — full enum with all future reasons defined now

**Application** (`RetailOS.Application/`):
- `Products/DTOs/ProductDTOs.cs` — `CreateProductRequest`, `UpdateProductRequest`, `UpdateProductStatusRequest`, `ProductResponse`, `ProductListResponse`
- `Products/Interfaces/IProductService.cs`
- `Products/Validators/CreateProductRequestValidator.cs`, `UpdateProductRequestValidator.cs`

**Infrastructure** (`RetailOS.Infrastructure/`):
- `Catalog/ProductService.cs` — implements `IProductService`
- `Persistence/Configurations/ProductConfiguration.cs`

**API**:
- `ProductsController.cs` — CRUD + barcode lookup + status patch + soft-delete

**Tests**:
- `tests/RetailOS.IntegrationTests/Catalog/ProductCrudTests.cs`
- `tests/RetailOS.IntegrationTests/Catalog/ProductValidationTests.cs`

---

### Phase C: Bulk Import (US3)

**Domain**:
- `Entities/InventoryTransaction.cs` : `ITenantEntity` (no soft-delete — ledger is immutable)

**Application** (`RetailOS.Application/`):
- `Products/DTOs/ImportDTOs.cs` — `ImportPreviewResponse`, `ImportRowError`, `ImportCommitResponse`
- `Products/Interfaces/IProductImportService.cs`

**Infrastructure** (`RetailOS.Infrastructure/`):
- `Catalog/ProductImportService.cs` — parse (CSV/XLSX) → validate (same `ProductValidator`) → return preview or commit
- `Persistence/Configurations/InventoryTransactionConfiguration.cs`

**API**:
- Add `POST /api/products/import/preview` and `POST /api/products/import/commit` to `ProductsController.cs`
- Add `[RequestSizeLimit(5_242_880)]` to import endpoints

**Tests**:
- `tests/RetailOS.IntegrationTests/Catalog/BulkImportTests.cs` — uses in-memory CSV/XLSX byte streams (no file system I/O needed)

---

### Phase D: Opening Stock (US4)

**Application** (`RetailOS.Application/`):
- `Inventory/DTOs/OpeningStockDTOs.cs` — `RecordOpeningStockRequest`, `BulkOpeningStockRequest`, `OpeningStockResponse`, `BulkOpeningStockResponse`
- `Inventory/Interfaces/IInventoryService.cs`
- `Inventory/Validators/RecordOpeningStockRequestValidator.cs`

**Infrastructure** (`RetailOS.Infrastructure/`):
- `Inventory/InventoryService.cs` — implements `IInventoryService`; enforces one-per-product via partial unique index catch

**API**:
- `InventoryController.cs` — `POST /api/inventory/opening-stock`, `POST /api/inventory/opening-stock/bulk`, `GET /api/inventory/opening-stock`

**Tests**:
- `tests/RetailOS.IntegrationTests/Catalog/OpeningStockTests.cs`

---

### Phase E: Migration & Cross-Cutting

- Generate and apply EF Core migration `AddCatalogAndInventoryLedger`
- Add functional unique indexes via `migrationBuilder.Sql()` in migration
- Register all new services in `DependencyInjection.cs`
- Update Swagger tags/grouping
- Write `CatalogTenantIsolationTests` verifying Store A/B separation across all catalog entities
- Write `EntityLifecycleTests` verifying deactivated category/unit cannot be used in new product creation

---

## Dependency Order

```
SoftDeletableEntity
    ↓
Category, Unit
    ↓
Product (requires Category + Unit)
    ↓
InventoryTransaction (requires Product)
    ↓
BulkImport (requires Product validation)
    ↓
OpeningStock (requires Product + InventoryTransaction)
```

---

## Verification Plan

1. `dotnet build` — no errors or warnings
2. `dotnet ef migrations add AddCatalogAndInventoryLedger` — migration generated cleanly
3. `dotnet ef database update` — all indexes created
4. `dotnet test` — all existing 21 tests still pass + new catalog tests pass
5. Manual validation of all Quickstart scenarios via HTTP client (Swagger or curl)

---

## Artifacts Generated

| Artifact | Path |
|---|---|
| Specification | `specs/002-catalog/spec.md` |
| Research decisions | `specs/002-catalog/research.md` |
| Data model | `specs/002-catalog/data-model.md` |
| Categories API contract | `specs/002-catalog/contracts/categories.md` |
| Units API contract | `specs/002-catalog/contracts/units.md` |
| Products API contract | `specs/002-catalog/contracts/products.md` |
| Opening Stock API contract | `specs/002-catalog/contracts/opening-stock.md` |
| Quickstart guide | `specs/002-catalog/quickstart.md` |
| Quality checklist | `specs/002-catalog/checklists/requirements.md` |

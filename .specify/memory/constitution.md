<!-- SYNC IMPACT REPORT
Version change: 1.0.0 → 1.1.0
Modified principles:
  - Principle I: priority order split into Absolute Constraints + Quality Tradeoffs
  - Principle II: added clarification on ledger pattern vs. event sourcing prohibition
  - Security & Multi-tenancy: tenant isolation mechanism specified explicitly;
                               rate limiting upgraded from SHOULD to MUST for auth endpoints
  - Domain & Business Rules / Costing: added decimal precision + rounding policy
  - Domain & Business Rules / Soft-Delete: scope expanded to all entities with historical records
  - Domain & Business Rules / Module Completion Gate: converted to checklist format;
                                                        added human review requirement for critical modules
Added sections:
  - Concurrency & Idempotency (new section under Domain & Business Rules)
  - ETA Electronic Invoice added to Out of Scope list
  - Testcontainers policy added to Quality & Engineering Standards
Removed sections: N/A
Follow-up TODOs: None — all TODOs closed.
  RATIFICATION_DATE resolved: 2026-09-01 (confirmed as official project start date)
-->

# Retail OS — Backend Constitution

## Absolute Constraints

The following are not tradeoffs or priorities — they are non-negotiable conditions.
Every implementation decision MUST satisfy all three simultaneously:

- **Correctness**: Business calculations, inventory mutations, and financial records MUST produce
  accurate, deterministic results. Incorrect behavior is never acceptable in exchange for simplicity.
- **Security**: Authentication, authorization, and data protection MUST be enforced. Security gaps
  cannot be deferred or traded for velocity.
- **Tenant Isolation**: Data belonging to one store MUST never be accessible or mutable by another
  store under any code path. This constraint is structural, not disciplinary.

## Quality Tradeoffs

When making design decisions within the bounds of the Absolute Constraints above, prefer in this order:

> Simple → Reliable → Readable → Maintainable → Scalable

Do not sacrifice simplicity for imaginary scale. Do not sacrifice maintainability for cleverness.

---

## Core Principles

### I. Simplicity First (KISS)

Solutions MUST be simple, readable, and maintainable before being clever, abstract, or complicated.
Complexity MUST NOT be introduced without a real, demonstrated requirement.

### II. No Premature Abstraction (YAGNI)

The system MUST NOT implement features before they are needed. The following are explicitly
prohibited unless a current requirement demands them: CQRS, event sourcing, message brokers,
distributed caching, microservices, complex domain event infrastructure, and generic enterprise
frameworks.

> **Clarification — Ledger Pattern vs. Event Sourcing**:
> The Inventory and Financial Ledger sections below require append-only transaction tables
> (`inventory_transactions`, `customer_account_transactions`, `supplier_account_transactions`).
> This is a **ledger pattern at the aggregate level** — not Event Sourcing as a system architecture.
> There is no general event store, no replay infrastructure, no event bus, and no CQRS.
> The prohibition on Event Sourcing remains fully in effect at the architectural level.
> These ledger tables are specific domain design decisions, not an architectural exception.

### III. DRY Without Over-Engineering

Business logic MUST NOT be duplicated. Abstractions MUST NOT be created prematurely.
Real duplication MUST be identified first; abstraction is applied only when it genuinely
improves maintainability. Giant generic abstractions to avoid a few repeated lines are prohibited.

### IV. SOLID Pragmatically Applied

SOLID principles MUST be applied with judgment — particularly: Single Responsibility,
Dependency Inversion, Open/Closed where genuinely useful, and Interface Segregation where
interfaces provide real value. Every class MUST NOT become an interface just because SOLID exists.
Unnecessary layering (e.g., IProductFactory + ProductFactory + IProductManager + ProductManager
for a simple use case) is prohibited.

### V. Strong Typing & Clean Contracts

EF Core entities MUST NOT be exposed directly from API controllers. Every request and response
MUST have a clear DTO/contract (e.g., `CreateProductRequest`, `ProductResponse`). Dynamic or
untyped structures MUST NOT be used when a proper type can be defined. Magic values
(hard-coded role names, status strings, numeric thresholds) MUST use enums, constants, or
value objects.

### VI. Business Integrity

Critical business operations (sale, purchase, payment, inventory mutation) MUST be atomic via
database transactions. Financial calculations MUST live in the backend; the frontend MUST only
display backend results. All inventory mutations MUST go through a centralized inventory service —
never from controllers directly. Approved financial transactions MUST NOT be hard-deleted; they
MUST be voided, cancelled, or reversed with audit information and reason captured.

### VII. Tenant Isolation is Structural

Tenant identity MUST be derived from the authenticated JWT context — never trusted from client
input. See the Security & Multi-tenancy section for the required enforcement mechanism.

---

## Architecture & Technology

### Stack

| Layer | Technology |
|---|---|
| Language | C# (.NET — latest stable LTS) |
| Framework | ASP.NET Core Web API |
| ORM | Entity Framework Core + Npgsql |
| Database | PostgreSQL |
| Auth | ASP.NET Core Identity, JWT access tokens, Refresh tokens |
| API Style | REST + OpenAPI/Swagger |
| Testing | xUnit, FluentAssertions, Testcontainers (PostgreSQL), Integration tests for core flows |

### Project Structure

```
src/
├── RetailOS.Api/
├── RetailOS.Application/
├── RetailOS.Domain/
├── RetailOS.Infrastructure/
└── RetailOS.Shared/
```

The architecture MUST be a **Simple Modular Monolith**. Microservices MUST NOT be introduced
in this phase. Business functionality MUST be organized into feature modules:
Auth, Stores, Users, Products, Categories, Units, Suppliers, Customers, Purchases,
Inventory, Sales, Payments, Expenses, Cash, Reports, Dashboard, Common.

### Database Conventions

- Table and column names: `snake_case` — MUST be configured explicitly in `OnModelCreating`
  from Phase 1 via a naming convention (e.g., `UseSnakeCaseNamingConvention()`).
  Deferring this causes migration conflicts that are expensive to fix later.
- Every schema change MUST be represented by a named EF Core migration.
- Migrations MUST have meaningful names (e.g., `AddStoresAndUsers`, `AddInventoryTransactions`).
- Database-level constraints MUST be used for required fields, foreign keys, and uniqueness.
- Indexes MUST be created based on real query patterns
  (e.g., `(store_id)`, `(store_id, barcode)`, `(store_id, created_at)`).

### API Conventions

- RESTful resource-based naming (`/api/products`, `/api/sales`).
- Business commands that cannot be expressed as CRUD MAY use action endpoints
  (e.g., `POST /api/sales/{id}/void`, `POST /api/purchases/{id}/return`).
- All potentially large collections MUST support server-side pagination.
- Error responses MUST follow a consistent format:
  `{ "success": false, "message": "...", "code": "DOMAIN_ERROR_CODE", "errors": [] }`.
- Stack traces, database internals, and secrets MUST NOT be exposed to clients.

---

## Domain & Business Rules

### Inventory

Inventory MUST use a **transaction model** with typed movement reasons:
`OPENING_BALANCE`, `PURCHASE`, `SALE`, `PURCHASE_RETURN`, `SALE_RETURN`,
`DAMAGE`, `LOSS`, `ADJUSTMENT`, `TRANSFER`.
The transaction history is the source of truth. Inventory MUST NOT be treated as a
random mutable number. The `AllowNegativeStock` store setting MUST be respected; default is `false`.

### Costing

Purchase cost history MUST be preserved per purchase item — historical costs MUST NOT be
overwritten. Inventory valuation and COGS MUST use **Weighted Average Cost** unless a later
business decision changes this. The calculation MUST be deterministic and testable.

**Decimal & Rounding Policy (MANDATORY)**:
- All monetary and cost values MUST use `decimal` (C#). `float` and `double` are prohibited
  for any financial calculation.
- Database column precision: `numeric(19,4)` for prices and payment amounts;
  `numeric(19,6)` for intermediate WAC calculations.
- Rounding rule: `MidpointRounding.AwayFromZero` (standard commercial rounding).
- Display rounding: round to 2 decimal places at the presentation layer only —
  never truncate intermediate calculation results.
- Violation of this policy is treated as a correctness bug, not a style issue.

### Financial Ledgers

Customer and supplier accounts MUST use a ledger/transaction model — not a single mutable
balance field. Transaction types:
- Customer: `SALE`, `PAYMENT`, `SALE_RETURN`, `ADJUSTMENT`
- Supplier: `PURCHASE`, `PAYMENT`, `PURCHASE_RETURN`, `ADJUSTMENT`

The current balance MUST be derivable and auditable from transaction history.

### Concurrency & Idempotency

**Race Condition Protection on Stock**:
Stock-affecting operations (sale, purchase receipt, adjustment, return) MUST use one of:
- **Optimistic concurrency**: a `RowVersion` or `xmin` column on the inventory balance entity,
  with EF Core `[ConcurrencyCheck]` and application-level retry on conflict.
- **Pessimistic locking**: `SELECT ... FOR UPDATE` via raw SQL or interceptor for
  high-contention scenarios (e.g., the last unit of a product).

The chosen strategy MUST be consistent across all inventory-mutating services.

**Idempotency on High-Risk Endpoints**:
The following endpoints MUST support an `Idempotency-Key` request header:
`POST /api/sales`, `POST /api/purchases`, `POST /api/payments`.

Implementation: a shared `idempotency_keys` table with columns
`(key, store_id, endpoint, response_snapshot, created_at)` and a TTL policy.
A repeated request with the same key MUST return the original response without re-executing.

### Product Import

Manual product creation and bulk product import MUST share the same core business rules
and validation logic. Two separate rule sets for the same domain concept are prohibited.
The bulk import workflow MUST follow:
Upload → Parse → Validate → Preview → User Confirmation → Commit → Result Summary.

### Soft-Delete & Entity Lifecycle

Approved financial transactions MUST be voided/reversed — never hard-deleted.

Additionally, **any entity that has historical records linked to it** MUST NOT be
hard-deleted. This includes: Products, Customers, Suppliers, Categories (when in use),
Units (when in use), and Supplier Representatives.
These entities MUST be deactivated (`IsActive = false`) or archived.
Hard deletion of such entities would break referential integrity and corrupt historical reports.

### Profit Model

```
Revenue − COGS = Gross Profit
Gross Profit − Operating Expenses = Operating Profit
```

"Net Profit" MUST NOT be used unless the full accounting definition is supported.
All dashboard and report values MUST be derived from persisted transactional data — no invented figures.

### Module Completion Gate (Definition of Done)

A module is complete only when **all** of the following are checked off:

- [ ] Business requirements understood and confirmed against constitution
- [ ] Domain model defined (entities, value objects, relationships)
- [ ] Database relationships and migrations defined
- [ ] Validation implemented (API boundary + domain layer)
- [ ] Authorization implemented (role + permission checks)
- [ ] Tenant isolation verified (global query filter active and tested)
- [ ] Concurrency protection implemented where stock or balances are mutated
- [ ] Core service/use case implemented
- [ ] API endpoints implemented
- [ ] Error handling implemented (consistent error format)
- [ ] Relevant tests implemented (xUnit unit tests + Testcontainers integration tests)
- [ ] Swagger/OpenAPI updated and accurate
- [ ] EF Core migration created with meaningful name
- [ ] **Human review required** for Inventory, Sales, and Payments modules before merge
- [ ] Integration verified with all prior modules

---

## Quality & Engineering Standards

### Testing

Tests MUST focus on business rules that can cause financial or inventory corruption.

High-priority coverage: Authentication, Multi-tenancy isolation, Concurrency on stock mutations,
Product rules (including bulk import validation), Purchase/inventory flows,
Sales/COGS/profit calculations, Cash register, Dashboard calculated figures.

100% coverage as a vanity metric is explicitly rejected.

**Integration Test Infrastructure**:
Integration tests MUST run against a real PostgreSQL instance.
The required tool is **Testcontainers for .NET** (spins up a real Postgres container per test run).
EF Core InMemory provider is permitted only for pure unit tests where no database constraints
are involved. Using InMemory for integration tests is prohibited — it does not enforce
referential integrity, uniqueness constraints, or check constraints, producing false confidence.

### Performance

- `AsNoTracking()` MUST be used for read-only queries.
- N+1 queries are prohibited.
- Projections MUST be used for read-heavy endpoints where appropriate.
- Unnecessary `Include()` chains are prohibited.
- PostgreSQL MUST perform filtering and sorting — loading large datasets into C# memory to filter
  is prohibited.
- Performance optimization MUST be evidence-driven; premature optimization is prohibited.

### Async & Cancellation

All database/network operations MUST use `async/await` with `CancellationToken`.
Threads MUST NOT be blocked with synchronous database calls.

### Logging & Configuration

Structured logging MUST be used. Passwords, JWT secrets, refresh tokens, and sensitive customer
data MUST NOT be logged. Connection strings, JWT secrets, API keys, and credentials MUST NOT be
hard-coded; configuration/environment variables/user secrets MUST be used.
Development and Production environments MUST be supported without source-code changes.

---

## Security & Multi-Tenancy

### Tenant Isolation Mechanism

Tenant isolation MUST be implemented through the following explicit stack — discipline alone is
insufficient across multiple agent sessions:

```
JWT Claims → IStoreContext (scoped service, resolves StoreId)
                 ↓
EF Core Global Query Filter (HasQueryFilter on every multi-tenant entity)
                 ↓
SaveChanges Interceptor (rejects any entity with missing or mismatched StoreId)
```

This means:
- Every entity participating in multi-tenancy MUST have a `StoreId` property.
- `HasQueryFilter` MUST be applied to every such entity in `OnModelCreating`.
- The `SaveChanges` interceptor MUST validate `StoreId` before any write is committed.
- Tests MUST verify that a token from Store A cannot read or write Store B data.

### Authentication & Authorization

- Password hashing MUST use standard ASP.NET Core Identity mechanisms.
- JWT validation MUST be enforced on all protected endpoints.
- Input validation MUST occur at the API boundary; business rule enforcement in the
  application/domain layer.
- CORS MUST be configured securely and explicitly.
- Secrets MUST NOT be committed to source control.
- Least privilege MUST be applied to all roles.

### Rate Limiting

- `POST /api/auth/login` and `POST /api/auth/refresh`: **MUST** have rate limiting
  (recommended: 5 requests/minute per IP). These are the primary brute-force targets.
- Other sensitive endpoints (e.g., bulk import, payment): SHOULD have rate limiting
  based on operational judgment.

### Initial Roles

`Owner` · `Manager` · `Cashier` · `InventoryClerk`

Owner has full control. Role-based and permission-based authorization MUST be enforced.
Cashiers MUST NOT access sensitive financial reports or void approved records unless
explicitly permitted by a store Owner or Manager.

---

## Development Process

### Phased Implementation Order

| Phase | Scope |
|---|---|
| 1 — Foundation | Solution setup, config, PostgreSQL, EF Core (snake_case from day 1), error handling, logging, auth, store/tenant context, users/roles |
| 2 — Catalog | Categories, Units, Products (manual + bulk import), Opening Stock |
| 3 — Business Operations | Suppliers, Reps, Customers, Purchases, Inventory, Sales, Payments, Balances, Expenses, Cash Register, Returns |
| 4 — Reporting | Sales, Purchase, Inventory, Profit, Customer/Supplier balances, Cash reports |
| 5 — Dashboard | Summary KPIs, stock alerts, top/slow products, financial indicators |

### Module-by-Module Rule

Modules MUST be implemented one at a time in dependency order. The next module MUST NOT
be started until the current module satisfies the Module Completion Gate above.
The entire application MUST NOT be generated in one uncontrolled pass.

### Before Coding Any Module

The agent MUST inspect the existing project to identify: what already exists, what can be reused,
what conflicts, what dependencies exist, and what database relationships are required.
Working code MUST NOT be overwritten blindly. Duplicate services or models for existing
domain concepts MUST NOT be created.

### Out of Scope (Current Phase)

The following are explicitly deferred — not forgotten:

- React frontend, React Native app
- Customer online store, Customer mobile app
- Supplier representative app, Digital supplier ordering
- Advanced AI/ML, Demand forecasting
- Marketplace, Online payments, Delivery management
- Multi-branch management
- Microservices, CQRS, Event sourcing, Redis, Message brokers
- **ETA Electronic Invoice Integration** (الفاتورة/الإيصال الإلكتروني):
  Deferred to a future phase. The Sales domain model MUST preserve placeholder fields
  (`eta_uuid`, `submission_status`) without activating them. This affects the Sales entity
  data model — the team MUST remain aware this is deferred, not forgotten, as ETA
  requirements expand over time.

The domain model MUST be designed so these features are possible later without requiring
a rewrite of the business core.

---

## Governance

This constitution supersedes all other implementation guidelines and ad-hoc instructions.
Any amendment MUST:

1. Identify the principle being changed and its rationale.
2. Increment the version per semantic versioning:
   - MAJOR: incompatible governance changes or principle removals.
   - MINOR: new principles, sections, or material expansions.
   - PATCH: clarifications, wording fixes, non-semantic refinements.
3. Update `LAST_AMENDED_DATE`.
4. Be reflected in the Sync Impact Report comment at the top of this file.

All feature specs and implementation plans MUST be verified for compliance with this
constitution before execution. Conflicts between a new requirement and this constitution
MUST be resolved by choosing the smallest safe architectural change that preserves existing
behavior, with the decision documented.

Note: Technology stack details, folder structures, and phased implementation plans are
subject to evolution and may be maintained in `plan.md` as the project progresses.
Constitutional amendments are required only for changes to principles and governance rules.

**Version**: 1.1.0 | **Ratified**: 2026-09-01 | **Last Amended**: 2026-09-01

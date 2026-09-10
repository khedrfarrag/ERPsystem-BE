# Research: Backend Foundation (Phase 1)

**Feature**: 001-backend-foundation | **Date**: 2026-09-01

All decisions below are resolved. No NEEDS CLARIFICATION markers remain.

---

## Decision 1: Authentication Architecture

**Decision**: ASP.NET Core Identity + JWT Bearer + server-side refresh token rotation

**Rationale**:
- ASP.NET Core Identity provides battle-tested password hashing (PBKDF2), user management,
  and role/claim infrastructure — no need to build from scratch.
- JWT Bearer is stateless for access tokens (15-min TTL); server-side refresh tokens
  (stored in DB) allow explicit revocation, which stateless-only JWT cannot provide.
- Single-use refresh token rotation: each refresh issues a new token and invalidates the old,
  limiting the blast radius of a stolen token to one use cycle.

**Alternatives considered**:
- Cookie-based sessions: Rejected — not suitable for a REST API consumed by mobile/web clients.
- OAuth2 / external identity provider: Rejected — adds external dependency and complexity
  not justified at this stage. Can be added later without architectural change.
- Fully stateless JWT (no server-side refresh store): Rejected — makes token revocation
  impossible without blacklisting, which defeats the purpose.

---

## Decision 2: Multi-Tenant Isolation Mechanism

**Decision**: EF Core Global Query Filters (HasQueryFilter) + scoped IStoreContext + SaveChanges interceptor

**Rationale**:
- `HasQueryFilter` on every multi-tenant entity ensures the WHERE clause is always applied
  automatically, even if a developer forgets to add it manually. Structural, not disciplinary.
- `IStoreContext` is a scoped service that resolves `StoreId` from the authenticated user's
  JWT claims at request time. Controllers and services never read `StoreId` from client input.
- The `TenantSaveChangesInterceptor` adds a final safety net: any entity with a missing or
  mismatched `StoreId` causes the entire `SaveChanges` call to fail before hitting the DB.
- This three-layer approach means a single forgotten `Where(x => x.StoreId == storeId)`
  cannot cause a data leak.

**Alternatives considered**:
- Manual `.Where(x => x.StoreId == storeId)` in every query: Rejected — error-prone,
  especially across 10+ business modules and many agent sessions.
- Separate database per tenant: Rejected — over-engineering at this scale; connection
  management complexity not justified for 10–100 stores.
- Row-level security in PostgreSQL: Rejected — adds operational complexity and makes
  testing harder without significant benefit over EF Core filters at this scale.

---

## Decision 3: Project Structure — Modular Monolith (4+1 projects)

**Decision**: `RetailOS.Api` / `RetailOS.Application` / `RetailOS.Domain` /
`RetailOS.Infrastructure` / `RetailOS.Shared`

**Rationale**:
- Clear separation allows future extraction to microservices if ever needed without
  a full rewrite — domain logic is already isolated.
- Domain project has zero infrastructure dependencies; Application depends only on Domain
  abstractions; Infrastructure implements them. This keeps the dependency graph clean.
- 5 projects is the minimum viable modular monolith for this domain size. Fewer projects
  would mix concerns that will become painful as business modules grow.

**Alternatives considered**:
- Single project: Rejected — mixing controllers, EF entities, and business logic makes
  refactoring painful as modules multiply.
- Feature-slice architecture: Considered — valid alternative, but the layered approach
  is more familiar to most .NET developers and better matches the constitution's emphasis
  on readability and maintainability.

---

## Decision 4: Database Naming Convention — snake_case from Day 1

**Decision**: Configure `UseSnakeCaseNamingConvention()` (EFCore.NamingConventions package)
in `OnModelCreating` before any migration is created.

**Rationale**:
- PostgreSQL convention is snake_case; PascalCase table names require quoted identifiers
  in raw SQL and tools (psql, pgAdmin, DataGrip), reducing DX significantly.
- If applied after migrations exist, all existing migrations must be regenerated or
  a costly rename migration must be created. Applying it on Day 1 costs nothing.

**Alternatives considered**:
- PascalCase (EF Core default): Rejected — violates PostgreSQL convention and the
  constitution's explicit snake_case requirement.
- Manual `ToTable()` / `HasColumnName()` on every entity: Rejected — verbose and
  error-prone; a convention plugin is the correct tool.

---

## Decision 5: Structured Logging — Serilog

**Decision**: Serilog with `WriteTo.Console(formatter: new JsonFormatter())` for development;
configurable sinks (file, cloud) for production via `appsettings.json`.

**Rationale**:
- Serilog is the most widely adopted structured logging library in the .NET ecosystem,
  with excellent ASP.NET Core integration and a rich sink ecosystem.
- JSON output in development allows easy parsing; same log events can be shipped to
  Elasticsearch, Seq, or Application Insights in production without code changes.
- `UseSerilogRequestLogging()` replaces the default ASP.NET Core request logging with
  a single structured log event per request, reducing noise.

**Alternatives considered**:
- Microsoft.Extensions.Logging only: Rejected — lacks structured output and provider
  flexibility without additional configuration.
- NLog: Valid alternative, but Serilog has broader adoption and better documentation.

---

## Decision 6: Error Handling — Global Middleware + ProblemDetails

**Decision**: Custom `ExceptionHandlingMiddleware` that catches all unhandled exceptions
and returns a consistent JSON response:
```json
{ "success": false, "message": "...", "code": "DOMAIN_ERROR_CODE", "errors": [] }
```
Domain exceptions (`DomainException`, `NotFoundException`, `ForbiddenException`) map to
specific HTTP status codes. Unexpected exceptions return 500 without stack trace details.

**Rationale**:
- Centralizing error mapping means no controller needs try/catch for domain exceptions.
- Consistent error format (from constitution) is enforced in one place.
- ProblemDetails (RFC 7807) is the .NET 8 standard — the custom format wraps it or
  follows the same spirit, keeping it compatible with client expectations.

**Alternatives considered**:
- Action filters for exception handling: Rejected — does not catch middleware exceptions.
- `UseExceptionHandler` built-in: Sufficient for generic 500s, but lacks domain-exception
  to HTTP-status mapping logic.

---

## Decision 7: Rate Limiting — .NET 8 Built-in Rate Limiter

**Decision**: `Microsoft.AspNetCore.RateLimiting` (built into .NET 8), with a Fixed Window
policy on `/api/auth/login` and `/api/auth/refresh`: 5 requests per 60 seconds per IP.

**Rationale**:
- Built-in rate limiter requires zero additional packages and is production-ready in .NET 8.
- Fixed Window is sufficient for brute-force protection on auth endpoints at this scale.
- Configuration lives in `appsettings.json` so limits can be adjusted without code changes.

**Alternatives considered**:
- AspNetCoreRateLimit (NuGet): Valid but adds an external package where the built-in
  solution is now sufficient.
- Token Bucket / Sliding Window: More sophisticated but unnecessary for the current use case.

---

## Decision 8: Integration Testing — Testcontainers + WebApplicationFactory

**Decision**: `Testcontainers.PostgreSql` spins up a real PostgreSQL container per test
collection. `WebApplicationFactory<Program>` provides the full ASP.NET Core pipeline.
`Microsoft.AspNetCore.Mvc.Testing` is used for HTTP-level integration tests.

**Rationale**:
- Constitution explicitly mandates Testcontainers; EF Core InMemory is prohibited for
  integration tests because it does not enforce DB constraints.
- Running against real PostgreSQL catches constraint violations, FK errors, migration
  correctness, and query filter behavior that InMemory silently ignores.
- `WebApplicationFactory` allows full end-to-end request testing without deploying a server.

**Alternatives considered**:
- EF Core InMemory: Explicitly prohibited by constitution.
- SQLite: Rejected — not PostgreSQL-compatible; different constraint behavior and no
  support for `numeric(19,4)` precision semantics.

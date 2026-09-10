# Implementation Plan: Backend Foundation (Phase 1)

**Branch**: `001-backend-foundation` | **Date**: 2026-09-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-backend-foundation/spec.md`

---

## Summary

Establish the complete backend foundation for Retail OS: .NET 8 ASP.NET Core Web API solution
with a modular monolith structure, PostgreSQL via EF Core, ASP.NET Core Identity with JWT + refresh
token authentication, role-based authorization (Owner/Manager/Cashier/InventoryClerk), and
structural multi-tenant isolation enforced via EF Core global query filters and a SaveChanges
interceptor. All infrastructure concerns (error handling, structured logging, configuration,
migrations) are established in this phase so that every subsequent business module builds on
a stable, tested, and secure foundation.

---

## Technical Context

**Language/Version**: C# 12 / .NET 8 (LTS)

**Primary Dependencies**:
- `Microsoft.AspNetCore` (Web API, Identity)
- `Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Serilog.AspNetCore` (structured logging)
- `FluentValidation.AspNetCore`
- `Swashbuckle.AspNetCore` (OpenAPI/Swagger)

**Testing**:
- `xunit` + `FluentAssertions`
- `Testcontainers.PostgreSql` (real Postgres for integration tests)
- `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory)

**Storage**: PostgreSQL 16 — snake_case naming convention applied from day 1

**Target Platform**: Linux-compatible server (Docker-deployable), Development on Windows

**Project Type**: Web Service — REST API (backend only, no frontend in this phase)

**Performance Goals**: < 200ms p95 for auth endpoints under normal load (10–50 stores initial scale)

**Constraints**:
- Secrets never in source control
- JWT access token TTL: 15 min (configurable)
- Refresh token TTL: 7 days (configurable), single-use rotation
- Rate limit: max 5 login/refresh attempts per IP per minute

**Scale/Scope**: Initial target 10–100 stores; architecture supports growth to 1,000+ without rewrite

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. KISS | ✅ | Modular monolith, no unnecessary layers |
| II. YAGNI | ✅ | No CQRS, event sourcing, message brokers, Redis |
| III. DRY | ✅ | Single auth/validation pipeline; no duplicated logic |
| IV. SOLID (pragmatic) | ✅ | No gratuitous interfaces; IStoreContext is genuinely needed |
| V. Strong Typing | ✅ | All request/response DTOs defined; entities not exposed |
| VI. Business Integrity | ✅ | Transactions on all writes; no direct DB from controllers |
| VII. Tenant Isolation | ✅ | Global query filter + SaveChanges interceptor — structural |
| Absolute: Correctness | ✅ | decimal + WAC policy applies from Foundation (migrations) |
| Absolute: Security | ✅ | Hashed passwords, JWT validation, rate limiting on auth |
| Absolute: Tenant Isolation | ✅ | Enforced structurally, not by discipline |
| Concurrency | ✅ | Optimistic concurrency on InventoryBalance (deferred to Phase 3 entity; interceptor pattern established here) |
| Decimal Policy | ✅ | `numeric(19,4)` / `numeric(19,6)` configured in EF Core from day 1 |
| Rate Limiting | ✅ | AspNetCoreRateLimit or built-in .NET 8 rate limiter on auth routes |
| Testcontainers | ✅ | Integration tests use real PostgreSQL |

**Complexity Tracking**: No constitution violations — no justification table needed.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-backend-foundation/
├── plan.md              ← this file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── contracts/
│   ├── auth.md
│   ├── stores.md
│   └── users.md
└── tasks.md             ← Phase 2 output (speckit-tasks)
```

### Source Code (repository root)

```text
src/
├── RetailOS.Api/                    ← ASP.NET Core Web API entry point
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── StoresController.cs
│   │   └── UsersController.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Extensions/
│   │   ├── ServiceCollectionExtensions.cs
│   │   └── ApplicationBuilderExtensions.cs
│   └── Program.cs
│
├── RetailOS.Application/            ← Use cases, DTOs, interfaces
│   ├── Auth/
│   │   ├── Commands/
│   │   │   ├── RegisterStoreCommand.cs
│   │   │   ├── LoginCommand.cs
│   │   │   ├── RefreshTokenCommand.cs
│   │   │   └── LogoutCommand.cs
│   │   └── AuthService.cs
│   ├── Stores/
│   │   └── StoreService.cs
│   ├── Users/
│   │   └── UserService.cs
│   └── Common/
│       ├── DTOs/
│       └── Interfaces/
│           └── IStoreContext.cs
│
├── RetailOS.Domain/                 ← Entities, enums, domain rules
│   ├── Entities/
│   │   ├── Store.cs
│   │   ├── User.cs
│   │   └── RefreshToken.cs
│   ├── Enums/
│   │   └── UserRole.cs
│   └── Common/
│       └── BaseEntity.cs
│
├── RetailOS.Infrastructure/         ← EF Core, Identity, JWT, Logging
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Interceptors/
│   │   │   └── TenantSaveChangesInterceptor.cs
│   │   ├── Configurations/
│   │   │   ├── StoreConfiguration.cs
│   │   │   └── UserConfiguration.cs
│   │   └── Migrations/
│   ├── Auth/
│   │   ├── JwtTokenService.cs
│   │   └── PasswordService.cs
│   ├── Services/
│   │   └── StoreContext.cs          ← IStoreContext implementation
│   └── DependencyInjection.cs
│
└── RetailOS.Shared/                 ← Shared primitives, result types
    ├── Result.cs
    ├── Error.cs
    └── Constants/
        └── Roles.cs

tests/
├── RetailOS.IntegrationTests/       ← Testcontainers + WebApplicationFactory
│   ├── Auth/
│   │   ├── AuthEndpointsTests.cs
│   │   └── TenantIsolationTests.cs
│   ├── Users/
│   │   └── UserManagementTests.cs
│   └── Infrastructure/
│       └── TestWebApplicationFactory.cs
└── RetailOS.UnitTests/
    └── Auth/
        └── TokenServiceTests.cs
```

**Structure Decision**: Single backend solution, 4-project modular monolith (Api / Application / Domain / Infrastructure / Shared). No frontend. Matches constitution exactly.

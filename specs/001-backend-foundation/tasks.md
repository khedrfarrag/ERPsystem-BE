# Tasks: Backend Foundation (Phase 1)

**Feature**: Backend Foundation (Phase 1)  
**Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize .NET 8 solution, project layout, and shared dependencies per implementation plan.

- [X] T001 Create .NET 8 solution and 5-project modular monolith structure (`src/RetailOS.Api`, `src/RetailOS.Application`, `src/RetailOS.Domain`, `src/RetailOS.Infrastructure`, `src/RetailOS.Shared`) in `RetailOS.sln`
- [X] T002 Add NuGet dependencies for EF Core, Npgsql, EFCore.NamingConventions, ASP.NET Core Identity, JWT Bearer, Serilog, and FluentValidation across project files in `src/`
- [X] T003 [P] Create test project structure (`tests/RetailOS.IntegrationTests`, `tests/RetailOS.UnitTests`) with Testcontainers.PostgreSql, FluentAssertions, and Microsoft.AspNetCore.Mvc.Testing in `tests/`
- [X] T004 [P] Implement `Result<T>` and `Error` response envelope primitives in `src/RetailOS.Shared/Result.cs` and `src/RetailOS.Shared/Error.cs`
- [X] T005 [P] Define application-wide role constants in `src/RetailOS.Shared/Constants/Roles.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T006 Define `BaseEntity` and `ITenantEntity` domain abstractions in `src/RetailOS.Domain/Common/BaseEntity.cs` and `src/RetailOS.Domain/Common/ITenantEntity.cs`
- [X] T007 Define `IStoreContext` interface in `src/RetailOS.Application/Common/Interfaces/IStoreContext.cs`
- [X] T008 Implement `StoreContext` resolving `StoreId` from JWT ClaimsPrincipal in `src/RetailOS.Infrastructure/Services/StoreContext.cs`
- [X] T009 Implement `TenantSaveChangesInterceptor` to enforce tenant ID assignment and validate no mismatched tenant writes in `src/RetailOS.Infrastructure/Persistence/Interceptors/TenantSaveChangesInterceptor.cs`
- [X] T010 Configure `AppDbContext` with PostgreSQL snake_case naming conventions, global query filters for `ITenantEntity`, and decimal precision rules (`numeric(19,4)` and `numeric(19,6)`) in `src/RetailOS.Infrastructure/Persistence/AppDbContext.cs`
- [X] T011 Implement `ExceptionHandlingMiddleware` to catch domain and unhandled exceptions into the standard error format in `src/RetailOS.Api/Middleware/ExceptionHandlingMiddleware.cs`
- [X] T012 Configure Serilog structured logging and suppress sensitive values in `src/RetailOS.Api/Program.cs`
- [X] T013 Configure ASP.NET Core built-in rate limiting (5 req/min per IP on auth endpoints) in `src/RetailOS.Api/Extensions/ServiceCollectionExtensions.cs`
- [X] T014 Setup integration test harness with `TestWebApplicationFactory` spinning up `Testcontainers.PostgreSql` in `tests/RetailOS.IntegrationTests/Infrastructure/TestWebApplicationFactory.cs`

**Checkpoint**: Foundation ready — database, multi-tenant isolation hooks, error handling, and test harness are active.

---

## Phase 3: User Story 1 — Store Owner Registers & Logs In (Priority: P1) 🎯 MVP

**Goal**: Allow a store owner to register a new store with an owner account, log in with JWT + refresh tokens, refresh their token with single-use rotation, and logout.

**Independent Test**: Register a store and owner, obtain tokens, call `/api/auth/me`, refresh the token, verify the old refresh token is rejected, and logout cleanly.

### Tests for User Story 1
- [X] T015 [P] [US1] Integration tests for store owner registration, login, and `/api/auth/me` in `tests/RetailOS.IntegrationTests/Auth/AuthRegistrationAndLoginTests.cs`
- [X] T016 [P] [US1] Integration tests for refresh token rotation, replay rejection, and logout in `tests/RetailOS.IntegrationTests/Auth/TokenRotationAndRevocationTests.cs`
- [X] T017 [P] [US1] Integration tests verifying rate limiting on `/api/auth/login` and `/api/auth/refresh` in `tests/RetailOS.IntegrationTests/Auth/AuthRateLimitingTests.cs`

### Implementation for User Story 1
- [X] T018 [P] [US1] Create `Store` entity and EF Core configuration in `src/RetailOS.Domain/Entities/Store.cs` and `src/RetailOS.Infrastructure/Persistence/Configurations/StoreConfiguration.cs`
- [X] T019 [P] [US1] Create `User` entity extending IdentityUser and EF Core configuration in `src/RetailOS.Domain/Entities/User.cs` and `src/RetailOS.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- [X] T020 [P] [US1] Create `RefreshToken` entity with SHA-256 token hashing and EF Core configuration in `src/RetailOS.Domain/Entities/RefreshToken.cs` and `src/RetailOS.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- [X] T021 [US1] Create and apply initial EF Core migration `AddFoundationAuthAndStore` in `src/RetailOS.Infrastructure/Persistence/Migrations/`
- [X] T022 [P] [US1] Implement JWT generator and password security service in `src/RetailOS.Infrastructure/Auth/JwtTokenService.cs` and `src/RetailOS.Infrastructure/Auth/PasswordService.cs`
- [X] T023 [US1] Implement `AuthService` handling registration (atomic store + owner creation), login, token rotation, and revocation in `src/RetailOS.Application/Auth/AuthService.cs`
- [X] T024 [US1] Implement `AuthController` exposing `/api/auth/register`, `/api/auth/login`, `/api/auth/refresh`, `/api/auth/logout`, and `/api/auth/me` per contracts in `src/RetailOS.Api/Controllers/AuthController.cs`
- [X] T025 [US1] Add input validation using FluentValidation for registration and login requests in `src/RetailOS.Application/Auth/Validators/`

**Checkpoint**: User Story 1 is fully functional and testable independently. Store owners can register and obtain secure, tenant-scoped tokens.

---

## Phase 4: User Story 2 — Tenant Isolation: Store A Cannot See Store B (Priority: P1)

**Goal**: Guarantee that Store A's authenticated context can never read, modify, or leak Store B's data under any endpoint or query path.

**Independent Test**: Register Store A and Store B, obtain tokens for both, and verify through automated tests that cross-tenant queries return 404 (Not Found) or are rejected at the data layer.

### Tests for User Story 2
- [X] T026 [P] [US2] Integration test verifying cross-tenant query rejection (Store A reading Store B entities returns 404) in `tests/RetailOS.IntegrationTests/Tenant/CrossTenantQueryIsolationTests.cs`
- [X] T027 [P] [US2] Integration test verifying cross-tenant write prevention (`TenantSaveChangesInterceptor` blocks saving an entity with mismatched StoreId) in `tests/RetailOS.IntegrationTests/Tenant/CrossTenantWriteIsolationTests.cs`

### Implementation for User Story 2
- [X] T028 [US2] Create `StoresController` exposing `GET /api/stores/current` and `PUT /api/stores/current` scoped strictly to `IStoreContext` in `src/RetailOS.Api/Controllers/StoresController.cs`
- [X] T029 [US2] Implement `StoreService` with update operations enforcing tenant boundaries in `src/RetailOS.Application/Stores/StoreService.cs`
- [X] T030 [US2] Add FluentValidation rules for store settings (currency ISO validation, timezone IANA validation) in `src/RetailOS.Application/Stores/Validators/UpdateStoreRequestValidator.cs`

**Checkpoint**: User Stories 1 and 2 work together. Multi-tenant isolation is confirmed through automated test coverage against real PostgreSQL.

---

## Phase 5: User Story 3 — Role-Based Access Control (Priority: P2)

**Goal**: Enforce authorization boundaries across Owner, Manager, Cashier, and InventoryClerk roles at the API gateway layer.

**Independent Test**: Log in with each role and execute actions that are permitted vs. forbidden; verify unauthorized requests receive 403 and unauthenticated requests receive 401.

### Tests for User Story 3
- [X] T031 [P] [US3] Integration test verifying role permission enforcement (Owner, Manager, Cashier, InventoryClerk) across endpoints in `tests/RetailOS.IntegrationTests/Auth/RoleBasedAccessControlTests.cs`
- [X] T032 [P] [US3] Integration test verifying unauthenticated requests return 401 across all protected routes in `tests/RetailOS.IntegrationTests/Auth/AuthenticationChallengeTests.cs`

### Implementation for User Story 3
- [X] T033 [P] [US3] Implement `UserRole` enum and role claim mapping in `src/RetailOS.Domain/Enums/UserRole.cs` and `src/RetailOS.Infrastructure/Auth/RoleClaimTransformation.cs`
- [X] T034 [US3] Configure ASP.NET Core Authorization policies (`RequireOwner`, `RequireManagerOrAbove`, `RequireStaff`) in `src/RetailOS.Api/Extensions/ServiceCollectionExtensions.cs`
- [X] T035 [US3] Apply authorization attributes to existing endpoints (`AuthController`, `StoresController`) ensuring only permitted roles access management routes in `src/RetailOS.Api/Controllers/`

**Checkpoint**: RBAC is active and verified across all protected endpoints.

---

## Phase 6: User Story 4 — Store Owner Manages Store Users (Priority: P2)

**Goal**: Enable store owners to create employees (Managers, Cashiers, InventoryClerks), update profiles, and activate/deactivate accounts with immediate authentication lockout.

**Independent Test**: As an Owner, create a Cashier; verify the Cashier can log in; deactivate the Cashier; verify subsequent login and refresh attempts are rejected with 403.

### Tests for User Story 4
- [X] T036 [P] [US4] Integration test for user listing, creation, and profile updating within store in `tests/RetailOS.IntegrationTests/Users/UserManagementTests.cs`
- [X] T037 [P] [US4] Integration test verifying that deactivating a user invalidates active sessions and rejects login in `tests/RetailOS.IntegrationTests/Users/UserDeactivationTests.cs`
- [X] T038 [P] [US4] Integration test verifying Owner self-deactivation is prohibited in `tests/RetailOS.IntegrationTests/Users/UserSelfDeactivationGuardTests.cs`

### Implementation for User Story 4
- [X] T039 [P] [US4] Implement user management DTOs (`CreateUserRequest`, `UpdateUserRequest`, `UserResponse`, `UserListResponse`) in `src/RetailOS.Application/Users/DTOs/`
- [X] T040 [US4] Implement `UserService` managing store-scoped users with pagination and active/deactive logic in `src/RetailOS.Application/Users/UserService.cs`
- [X] T041 [US4] Implement `UsersController` exposing `GET /api/users`, `POST /api/users`, `GET /api/users/{id}`, `PUT /api/users/{id}`, and `PATCH /api/users/{id}/status` in `src/RetailOS.Api/Controllers/UsersController.cs`
- [X] T042 [US4] Add FluentValidation rules for `CreateUserRequest` and `UpdateUserRequest` in `src/RetailOS.Application/Users/Validators/`

**Checkpoint**: User Story 4 is complete. Store owners can manage their staff accounts with strict tenant and role enforcement.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final hardening, documentation, and end-to-end validation.

- [X] T043 [P] Configure Swagger / OpenAPI with JWT Bearer security definitions in `src/RetailOS.Api/Program.cs`
- [X] T044 Implement health check endpoints (`/health/live`, `/health/ready` verifying DB connection) in `src/RetailOS.Api/Program.cs`
- [X] T045 Execute and verify all scenarios from `quickstart.md` using the automated test suite in `tests/RetailOS.IntegrationTests/`
- [X] T046 Verify module completion checklist against constitution Definition of Done in `specs/001-backend-foundation/checklists/requirements.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 completion — **BLOCKS all user stories**.
- **User Story 1 (Phase 3)**: Depends on Phase 2 — unlocks core authentication and store creation (**MVP**).
- **User Story 2 (Phase 4)**: Depends on Phase 3 (needs registered stores to test isolation).
- **User Story 3 (Phase 5)**: Depends on Phase 3 (needs auth tokens to verify role policies).
- **User Story 4 (Phase 6)**: Depends on Phase 3 and Phase 5 (needs Owner credentials and role policies).
- **Polish (Phase 7)**: Depends on all user stories (Phases 3–6) complete.

### Parallel Opportunities

- **Phase 1**: T003, T004, T005 can run in parallel after T001 and T002.
- **Phase 2**: T006, T007 can run in parallel; T011, T012, T013 can run in parallel once project references exist.
- **Phase 3**: Tests T015, T016, T017 can be written in parallel; entity definitions T018, T019, T020 can be created in parallel.
- **Phase 4**: Tests T026, T027 can run in parallel.
- **Phase 5**: Tests T031, T032 can run in parallel.
- **Phase 6**: Tests T036, T037, T038 can run in parallel.

---

## Implementation Strategy

### MVP First (User Story 1 Only)
1. Complete **Phase 1: Setup** (solution and packages).
2. Complete **Phase 2: Foundational** (DB context, tenant interceptor, error middleware).
3. Complete **Phase 3: User Story 1** (auth, JWT, refresh rotation).
4. **VALIDATE**: Run `AuthRegistrationAndLoginTests` and `TokenRotationAndRevocationTests` against Testcontainers Postgres.
5. Store registration and login MVP is ready.

### Incremental Delivery
- **Increment 1**: MVP (Setup + Foundation + US1) → Owners can register and log in.
- **Increment 2**: US2 (Tenant Isolation & Store settings) → Strict tenant data isolation verified.
- **Increment 3**: US3 (RBAC) → Role-restricted routes active.
- **Increment 4**: US4 (User Management) → Owners can manage store cashiers and managers.
- **Increment 5**: Polish (Swagger, Health checks, DoD verification) → Phase 1 ready for Phase 2 (Catalog).

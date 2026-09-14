# Tasks: Production Deployment & Container Orchestration (015-production-deployment)

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Status**: Complete / Validated

---

## Phase 1: Setup (Shared Infrastructure & Secrets Isolation)

**Purpose**: Project initialization, environment templates, and strict secrets isolation

- [X] T001 Configure production environment variable templates and secrets isolation rules in `system-BE/.gitignore` and `system-BE/.env.production.example`
- [X] T002 [P] Configure production environment variable templates and secrets isolation rules in `system-FE/.gitignore` and `system-FE/.env.production.example`
- [X] T003 [P] Setup production ASP.NET Core configuration template with sanitized runtime tokens in `src/RetailOS.Api/appsettings.Production.json`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core container orchestration, reverse proxy configuration, and web server baseline

**⚠️ CRITICAL**: Foundational tasks must complete before user story implementation and testing

- [X] T004 Define production multi-container orchestration topology and log rotation policies in `docker-compose.prod.yml`
- [X] T005 [P] Configure edge reverse proxy routing and automatic Let's Encrypt SSL/TLS rules in `Caddyfile`
- [X] T006 [P] Configure SPA web server reverse routing, Gzip compression, and HTTP security headers in `system-FE/nginx.conf`

**Checkpoint**: Foundation ready - container orchestration definitions and proxy routing established

---

## Phase 3: User Story 1 - One-Click Production Deployment with Automated HTTPS (Priority: P1) 🎯 MVP

**Goal**: Deliver an automated, single-command deployment workflow that builds and runs all services with auto-renewing SSL/TLS certificates over HTTPS.

**Independent Test**: Execute `./deploy.sh` on an Ubuntu VPS or local Docker host; verify all 4 containers start, transition to healthy status within 60s, and navigate to `https://domain.com` with valid TLS certificates.

### Implementation for User Story 1

- [X] T007 [P] [US1] Create multi-stage .NET 9 Alpine build and non-root execution configuration in `Dockerfile`
- [X] T008 [P] [US1] Create multi-stage Node 22 build and Nginx Alpine Slim container configuration in `system-FE/Dockerfile`
- [X] T009 [US1] Implement automated one-click production deployment workflow with health validation in `deploy.sh`
- [X] T010 [US1] Validate full container orchestration stack configuration using `docker compose -f docker-compose.prod.yml config`

**Checkpoint**: User Story 1 complete - one-click deployment script builds, validates, and runs the entire stack with automated HTTPS.

---

## Phase 4: User Story 2 - Zero-Downtime Automated Database Migrations (Priority: P1)

**Goal**: Ensure the backend container automatically applies pending EF Core migrations on container startup using a self-contained bundle before opening traffic.

**Independent Test**: Spin up backend against a fresh database; verify migration bundle runs, creates all tables, and reports ready status before accepting HTTP requests.

### Implementation for User Story 2

- [X] T011 [US2] Create standalone migration bundle execution and database readiness retry loop in `entrypoint.sh`
- [X] T012 [US2] Integrate entrypoint execution script into backend container entrypoint in `Dockerfile`
- [X] T013 [US2] Verify migration bundle execution and health probe transition in `src/RetailOS.Api/Controllers/HealthController.cs`

**Checkpoint**: User Stories 1 and 2 functional - zero-downtime automated migrations execute reliably on boot.

---

## Phase 5: User Story 3 - Zero-Trust Database Network Isolation (Priority: P2)

**Goal**: Confine the PostgreSQL database to an isolated internal Docker bridge network without exposing host port 5432 to the public internet.

**Independent Test**: Scan the host IP on port 5432 from an external network to confirm the port is closed/unreachable, while internal backend containers communicate without friction.

### Implementation for User Story 3

- [X] T014 [US3] Configure internal bridge network segmentation (`internal_backend_net` and `internal_frontend_net`) and isolate PostgreSQL port 5432 in `docker-compose.prod.yml`
- [X] T015 [US3] Verify database network access boundary and container service resolution in `docker-compose.prod.yml`

**Checkpoint**: User Story 3 complete - database port 5432 is strictly isolated from the public network.

---

## Phase 6: User Story 4 - Backward-Compatible API Versioning (Priority: P2)

**Goal**: Ensure all backend endpoints canonically support `/api/v1/...` while maintaining transparent fallback support for `/api/...` to avoid breaking existing clients.

**Independent Test**: Issue GET/POST requests to `/api/v1/auth/login` and `/api/auth/login`; confirm both return identical valid responses.

### Implementation for User Story 4

- [X] T016 [P] [US4] Configure dual routing attributes (`api/v1/[controller]` and `api/[controller]`) across all 21 controllers in `src/RetailOS.Api/Controllers/`
- [X] T017 [P] [US4] Update frontend API client configuration and base URL to target `/api/v1` in `system-FE/src/api/client.ts`
- [X] T018 [US4] Verify dual-route parity and TypeScript compilation in `system-BE` and `system-FE`

**Checkpoint**: User Story 4 complete - dual API versioning active and verified across backend and frontend.

---

## Phase 7: User Story 5 - Automated Backup and Disaster Recovery (Priority: P3)

**Goal**: Implement an automated scheduled database backup script that creates gzip-compressed dumps daily at 03:00 AM and prunes backups older than 14 days.

**Independent Test**: Run `./backup.sh`; verify `.sql.gz` dump is generated in `./backups/` and simulated files older than 14 days are automatically pruned.

### Implementation for User Story 5

- [X] T019 [US5] Implement automated daily database backup script with gzip compression and 14-day rolling retention in `backup.sh`
- [X] T020 [US5] Document automated backup crontab schedule installation and restoration procedures in `DEPLOYMENT_GUIDE.md`

**Checkpoint**: User Story 5 complete - scheduled backups and retention policy automated and documented.

---

## Phase 8: User Story 6 - Idempotent Production Master Seeding & First-Login Password Change (Priority: P3)

**Goal**: Seed essential master data (Store, Owner `owner@retailos.com`, core units) idempotently without dummy transactions, and force a password change on first login.

**Independent Test**: Start container in `Production` mode; log in as `owner@retailos.com`, confirm `mustChangePassword: true`, change password via `POST /api/v1/auth/change-password`, and verify flag is cleared.

### Implementation for User Story 6

- [X] T021 [P] [US6] Define production seeding interface in `src/RetailOS.Application/Common/Interfaces/IProductionDataSeeder.cs`
- [X] T022 [US6] Implement idempotent master data seeder for store, owner, and core units in `src/RetailOS.Infrastructure/Services/ProductionDataSeeder.cs`
- [X] T023 [P] [US6] Implement first-login password change logic and `MustChangePassword` claim clearing in `src/RetailOS.Infrastructure/Services/AuthService.cs`
- [X] T024 [US6] Expose password change endpoint `POST /api/v1/auth/change-password` and claim response in `src/RetailOS.Api/Controllers/AuthController.cs`
- [X] T025 [US6] Wire production seeding invocation into startup pipeline in `src/RetailOS.Api/Program.cs`

**Checkpoint**: User Story 6 complete - clean production master seeding and mandatory first-login password change enforced.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Production compilation validation, deployment handbook, and end-to-end verification

- [X] T026 [P] Finalize comprehensive enterprise deployment guide and secrets management instructions in `DEPLOYMENT_GUIDE.md`
- [X] T027 [P] Validate backend project compilation with `dotnet build src/RetailOS.Api/RetailOS.Api.csproj -t:Compile`
- [X] T028 [P] Validate frontend production bundle build with `npm run build` in `system-FE`
- [X] T029 Execute end-to-end quickstart validation sequence per `specs/015-production-deployment/quickstart.md`

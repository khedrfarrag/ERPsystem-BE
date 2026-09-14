# Implementation Plan: Production Deployment & Container Orchestration (015-production-deployment)

**Branch**: `015-production-deployment` | **Date**: 2026-09-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/015-production-deployment/spec.md`

---

## Summary

Deliver a production-ready, containerized deployment infrastructure utilizing Docker, Docker Compose, Nginx, and Caddy with automated Let's Encrypt SSL/TLS termination on a generic Ubuntu Linux VPS. The solution implements strict zero-trust database network isolation, automated database schema migrations via a standalone EF Core migration bundle on startup, backward-compatible dual-routing API versioning (`/api/v1/...` and `/api/...`), and a single-command automated deployment script (`./deploy.sh`).

---

## Technical Context

**Language/Version**: C# (.NET 9 Web API), TypeScript (React 18 / Vite 5), POSIX Shell (Alpine Linux / Bash).

**Primary Dependencies**:
- Backend: ASP.NET Core 9.0, Entity Framework Core 9.0 + Npgsql, Serilog, Swashbuckle OpenAPI.
- Frontend: React 18, Vite, Tailwind CSS, Lucide React, Axios.
- Infrastructure: Docker Compose 3.9, PostgreSQL 16 Alpine, Caddy 2 Alpine, Nginx Alpine Slim.

**Storage**: PostgreSQL 16 Alpine on persistent Docker named volume `pgdata:/var/lib/postgresql/data`.

**Testing**:
- Backend unit and integration tests (`dotnet test`).
- Production build verification (`dotnet build`, `npm run build`).
- Healthcheck probes (`/health/live`, `/health/ready`).

**Target Platform**: Linux Server (Ubuntu 22.04 LTS / 24.04 LTS) with Docker 24+ & Docker Compose 2.20+.

**Project Type**: Containerized Modular Monolith Web API + Single Page Application (SPA) Web Client + Edge Proxy.

**Performance Goals**:
- Full cold deployment from source to running HTTPS stack in < 5 minutes.
- Edge proxy overhead < 2ms latency.
- Backend memory footprint < 150MB on idle.
- Static SPA assets served with 1-year immutable caching and Gzip/Zstandard compression.

**Constraints**:
- Database port 5432 MUST NEVER be exposed to public host interfaces.
- Zero paid licenses or external SaaS dependencies required (100% free open-source stack).
- Container processes MUST execute as unprivileged non-root users (`USER $APP_UID`).

**Scale/Scope**:
- Single-node VPS architecture capable of handling 500+ concurrent retail requests, expandable to read-replicas or cluster configurations when business requires.

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Correctness**: Zero data corruption; migrations executed atomically inside database transactions.
- [x] **Security**: Non-root container users, database hidden behind internal bridge network, automated TLS encryption via Let's Encrypt.
- [x] **Tenant Isolation**: Preserved; `IStoreContext` and EF Core Global Query Filters are unchanged and active.
- [x] **Simplicity First (KISS)**: Simple 4-service Docker Compose stack without Kubernetes or premature cloud mesh complexity.
- [x] **No Premature Abstraction (YAGNI)**: Straightforward Caddy reverse proxy; no complex service meshes or distributed message queues.
- [x] **API Conventions**: Canonical `/api/v1/...` with transparent fallback for `/api/...`.
- [x] **Module Completion Gate**: All verification scenarios covered.

---

## Project Structure

### Documentation (this feature)

```text
specs/015-production-deployment/
├── spec.md              # Feature specification
├── plan.md              # This implementation plan
├── research.md          # Phase 0 architectural decisions
├── data-model.md        # Phase 1 infrastructure topology & services schema
├── quickstart.md        # Phase 1 runnable validation guide
├── checklists/
│   └── requirements.md  # Quality validation checklist
└── contracts/
    ├── docker-compose-contract.yaml # Container orchestration schema
    └── api-routes-contract.json     # Versioned API routes schema
```

### Source Code & Deployment Assets

```text
system-BE/
├── Dockerfile                  # Multi-stage .NET 9 Web API Alpine image
├── entrypoint.sh               # Migration bundle execution & startup script
├── docker-compose.prod.yml     # Multi-container production stack definition
├── Caddyfile                   # Automated Let's Encrypt SSL & edge proxy routing
├── deploy.sh                   # One-click Ubuntu VPS deployment script
├── .env.production.example     # Environment template
├── DEPLOYMENT_GUIDE.md         # Full operational handbook
└── src/RetailOS.Api/
    ├── appsettings.Production.json
    └── Controllers/            # All 21 controllers decorated with v1 dual routes

system-FE/
├── Dockerfile                  # Multi-stage Vite build + Nginx Alpine image
└── nginx.conf                  # SPA routing, Gzip compression & security headers
```

---

## Complexity Tracking

> **No constitutional violations detected. Clean, standard architecture.**

| Item | Decision | Justification |
|---|---|---|
| Caddy Reverse Proxy | Selected over Nginx + Certbot | Eliminates external cron jobs and certbot containers; native HTTP/3 and automated ACME renewals. |
| Standalone Migration Bundle | Selected over `MigrateAsync()` | Decouples migration execution from API startup; avoids runtime SDK dependencies and race condition locks. |
| Dual-Route Decorator | Selected over NuGet API versioning packages | Zero additional dependencies; completely transparent backward-compatibility without routing conflicts. |

# Phase 0 Research: Production Deployment & Container Orchestration (015-production-deployment)

## Research Findings & Architectural Decisions

---

### Decision 1: .NET 9 Multi-Stage Dockerfile (Alpine vs Debian)

- **Context**: The backend Web API needs a production container image optimized for minimal attack surface, fast deployment pulls, and strict security compliance.
- **Decision**: Multi-stage build with `mcr.microsoft.com/dotnet/sdk:9.0-alpine` as builder and `mcr.microsoft.com/dotnet/aspnet:9.0-alpine` as runtime.
- **Rationale**:
  1. Alpine reduces the runtime container image footprint from ~250MB (Debian) to under 110MB.
  2. Minimal installed utilities decrease vulnerabilities (CVEs) significantly.
  3. Includes built-in non-root user `app` (`$APP_UID: 1654`) out of the box in .NET 9.
  4. Layer caching: Copying `.sln` and `.csproj` files prior to `COPY . .` allows Docker to cache the entire NuGet restore layer. Subsequent code changes rebuild in < 15 seconds.
- **Alternatives Considered**:
  - *Debian Bookworm-slim*: Larger footprint; unnecessary package overhead for a stateless web API.
  - *Chiseled Ubuntu*: Ultra-minimal, but lacks `curl`/`wget` needed for Docker container healthchecks.

---

### Decision 2: Automated Database Migrations via Standalone Migration Bundle

- **Context**: In production, EF Core database migrations must run automatically on startup without requiring human terminal intervention and without installing the heavy .NET SDK or `dotnet-ef` global tools in the production runtime container.
- **Decision**: Compile an EF Core **Migration Bundle** (`dotnet ef migrations bundle`) during the SDK build stage, outputting a self-contained executable `/app/publish/bundle`.
- **Rationale**:
  1. A migration bundle is a single native Linux binary containing all migration logic, Entity models, and Npgsql drivers.
  2. Operates seamlessly inside the lightweight runtime image without any development tooling.
  3. Controlled via `entrypoint.sh` with a retry loop (5 attempts, 4-second exponential backoff), gracefully handling scenarios where the PostgreSQL container takes several seconds to become healthy.
  4. Prevents pipeline locks and avoids running arbitrary SQL scripts manually.
- **Alternatives Considered**:
  - *`context.Database.Migrate()` in Program.cs*: Can cause concurrency deadlocks if multiple API instances start simultaneously; ties application boot lifecycle directly to DB migration lock.
  - *Manual SQL script execution*: High risk of human error; breaks automated CI/CD pipeline principles.

---

### Decision 3: Reverse Proxy & Automated SSL (Caddy vs Nginx + Certbot)

- **Context**: Production web applications require HTTPS with valid SSL certificates, automatic renewal, HTTP/2 or HTTP/3 support, and reverse proxy routing to frontend and backend services.
- **Decision**: Utilize **Caddy v2 Alpine** as the edge reverse proxy.
- **Rationale**:
  1. Native automated TLS: Caddy automatically requests, verifies, installs, and auto-renews certificates via Let's Encrypt / ZeroSSL without requiring external certbot containers or cron scripts.
  2. Built-in HTTP/3 (QUIC) and HTTP/2 support out of the box.
  3. Concise, declarative configuration (`Caddyfile` is < 35 lines) compared to verbose multi-file Nginx SSL configs.
  4. Security headers (HSTS, X-Frame-Options, CSP) and compression (zstd, gzip) configured in single directives.
- **Alternatives Considered**:
  - *Nginx + Certbot*: Traditional, but requires dual containers, complex shared volume challenges for `/.well-known/acme-challenge/`, and cron renewal scripts.
  - *Traefik*: Powerful, but introduces unnecessary complexity (dynamic labels, dashboard) for a single-node VPS deployment.

---

### Decision 4: Zero-Trust Database Network Isolation

- **Context**: Protecting retail financial data, inventory stock, and customer accounts from ransomware and scanning attacks.
- **Decision**: Dual-bridge network architecture in `docker-compose.prod.yml`:
  1. `internal_backend_net`: Internal bridge (`internal: true`). Only connects `backend` and `database`.
  2. `internal_frontend_net`: Connects `edge_proxy`, `frontend`, and `backend`.
- **Rationale**:
  1. The database has **no port mappings** (`ports: 5432:5432` omitted). It cannot be reached or scanned from the internet.
  2. Even if the edge proxy is breached, the database is unreachable from the public DMZ network.
- **Alternatives Considered**:
  - *Single shared bridge network*: Functional, but lacks defense-in-depth segmentation.

---

### Decision 5: API Versioning (Dual-Routing v1)

- **Context**: As the system prepares for production release, backend routes must be versioned (`/api/v1/...`) to protect against breaking changes in future releases, without breaking existing frontend calls.
- **Decision**: Dual route decoration on all ASP.NET Core controllers:
  ```csharp
  [Route("api/v1/[controller]")]
  [Route("api/[controller]")]
  ```
- **Rationale**:
  1. Zero breaking changes: Any existing client or script continues working on `/api/...`.
  2. All new clients and the updated production frontend communicate over `/api/v1/...`.
  3. In the future, breaking changes can be published under `api/v2/[controller]` without altering v1 controllers.
- **Alternatives Considered**:
  - *Query string versioning (`?api-version=1.0`)*: Clunky URLs; awkward caching and routing in SPA clients.
  - *Immediate hard deprecation of `/api/...`*: Would risk regression during deployment transition.

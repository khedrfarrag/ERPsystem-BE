# Phase 1 Data Model: Production Deployment Architecture (015-production-deployment)

## Container & Infrastructure Topology

```text
Host VPS (Linux Ubuntu)
│
├── [Port 80/443 Public]
│   └── [Container] edge_proxy (Caddy 2 Alpine)
│       │
│       ├── internal_frontend_net (Bridge)
│       │   ├── [Container] frontend (Nginx Alpine + React SPA)
│       │   └── [Container] backend (RetailOS .NET 9 API on port 5000)
│       │
│       └── internal_backend_net (Isolated Internal Bridge)
│           ├── [Container] backend
│           └── [Container] database (PostgreSQL 16 Alpine, Port 5432 Internal)
│               └── [Named Volume] pgdata -> /var/lib/postgresql/data
```

---

## 1. Services Specification

| Service Name | Image / Build Context | Base Image | Internal Port | Exposed Host Port | Networks | Volume Mounts |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`database`** | `postgres:16-alpine` | Alpine Linux | `5432` | *None* (Hidden) | `internal_backend_net` | `pgdata:/var/lib/postgresql/data` |
| **`backend`** | `./system-BE` (Multi-stage) | `aspnet:9.0-alpine` | `5000` | *None* (Hidden) | `internal_backend_net`, `internal_frontend_net` | None |
| **`frontend`** | `../system-FE` (Multi-stage) | `nginx:alpine-slim` | `80` | *None* (Hidden) | `internal_frontend_net` | None |
| **`edge_proxy`** | `caddy:2-alpine` | Alpine Linux | `80, 443` | `80:80, 443:443, 443/udp` | `internal_frontend_net` | `caddy_data:/data`, `caddy_config:/config`, `./Caddyfile:/etc/caddy/Caddyfile:ro` |

---

## 2. Environment Variables Schema (.env)

| Variable Name | Type | Sensitivity | Default Value | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| `DOMAIN` | String (FQDN) | Public | `localhost` | Primary domain name for public web access and SSL certificate |
| `ACME_EMAIL` | String (Email) | Low | `admin@retailos.com` | Notification email for Let's Encrypt SSL certificate issuance |
| `POSTGRES_DB` | String | Low | `retailos_prod` | Database name created inside PostgreSQL instance |
| `POSTGRES_USER` | String | Low | `retailos_admin` | Database administrative role username |
| `POSTGRES_PASSWORD` | String | **Critical Secret** | *Required* | Database authentication password |
| `JWT_SECRET` | String (256-bit) | **Critical Secret** | *Required (>=32 chars)* | Cryptographic HMAC-SHA256 signing key for authentication tokens |
| `ASPNETCORE_ENVIRONMENT`| String | Low | `Production` | ASP.NET Core hosting profile |
| `GEMINI_API_KEY` | String | Secret | *Optional* | API Key for AI Invoice Scanner integration |
| `GEMINI_MODEL` | String | Low | `gemini-1.5-pro` | LLM model name for invoice OCR extraction |
| `TZ` | String | Low | `Africa/Cairo` | System and database local timezone setting |

---

## 3. Persistent Volumes

| Volume Name | Target Container Path | Host Driver | Backup Priority | Retention / Lifecycle |
| :--- | :--- | :--- | :--- | :--- |
| **`pgdata`** | `/var/lib/postgresql/data` | Local | **Critical** | Retained indefinitely across container restarts, image upgrades, and deployments |
| **`caddy_data`** | `/data` | Local | High | Persists issued TLS certificates and ACME account keys to avoid Let's Encrypt rate limits |
| **`caddy_config`**| `/config` | Local | Low | Caddy runtime configuration cache |

---

## 4. Health Check Probes

```yaml
database:
  test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-retailos_admin} -d ${POSTGRES_DB:-retailos_prod}"]
  interval: 10s
  timeout: 5s
  retries: 5
  start_period: 10s

backend:
  test: ["CMD", "curl", "-f", "http://localhost:5000/health/live"]
  interval: 15s
  timeout: 5s
  retries: 3
  start_period: 15s

frontend:
  test: ["CMD", "wget", "-q", "--spider", "http://localhost/health"]
  interval: 15s
  timeout: 5s
  retries: 3
```

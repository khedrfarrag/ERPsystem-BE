# Feature Specification: Production Deployment & Container Orchestration (015-production-deployment)

**Feature Branch**: `015-production-deployment`

**Created**: 2026-09-14

**Status**: Complete / Validated

**Input**: User description: "توثيق واعتماد مواصفات النشر والإنتاج الكامل، تشغيل الحاويات Docker Compose، عزل قاعدة بيانات PostgreSQL في شبكة داخلية آمنة، تشغيل الباك إند (.NET 9) بإصدارات API Versioning v1 والهجرة التلقائية للجداول، واجهة المستخدم React SPA مع Nginx، وبوابة الحماية وتجديد شهادات SSL التلقائية عبر Caddy/Let's Encrypt، مع توفير سكربتات النشر والأمان والنسخ الاحتياطي."

---

## Clarifications

### Session 2026-09-14

- Q: ما هي استراتيجية تدوير وحجم ملفات السجلات (Docker Log Rotation) التي ترغب في تطبيقها على خادم الإنتاج لمنع امتلاء مساحة القرص الصلب بمرور الوقت؟ → A: Option A (تدوير تلقائي: حد أقصى 20MB للملف مع الاحتفاظ بـ 3 نسخ سابقة لكل حاوية عبر `json-file` driver، مع توجيه سجلات PostgreSQL و Caddy و Nginx و .NET 9 مباشرة إلى stdout/stderr لمنع تضخم الأحجام التخزينية الدائمة).
- Q: ما هي آلية وجدولة النسخ الاحتياطي التلقائي لقاعدة البيانات (Automated Scheduled Backups) التي ترغب في تفعيلها على خادم الإنتاج لضمان عدم ضياع أي بيانات تجارية؟ → A: Option A (سكربت مجدول آلياً Cron Job ينفذ يومياً في الساعة 03:00 فجراً، مع ضغط النسخ بصيغة `.sql.gz` والاحتفاظ بآخر 14 يوماً وحذف ما هو أقدم منها تلقائياً لتوفير المساحة).
- Q: كيف ترغب في تهيئة الحساب الرئيسي وبيانات البداية (Initial Setup & Master Account) عند إطلاق قاعدة البيانات لأول مرة في بيئة الإنتاج؟ → A: Option A (تجهيز متجر نظيف: إنشاء حساب المالك الرئيسي الافتراضي `owner@retailos.com` بكلمة مرور مؤقتة، وإلزام تغيير كلمة المرور عند أول تسجيل دخول عبر Claim: `MustChangePassword` ونقطة نهاية `change-password`، وإنشاء الوحدات والتصنيفات الأساسية فقط بدون أي منتجات أو فواتير وهمية).

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One-Click Production Deployment with Automated HTTPS (Priority: P1)

As a DevOps Engineer or Store Owner deploying to a generic Ubuntu Linux VPS, I want to execute a single deployment script (`./deploy.sh`) that builds and runs all services (API, SPA Web Client, Database, and Edge Proxy) with automated, auto-renewing SSL/TLS certificates so that users can access the system securely over HTTPS (`https://domain.com`) without manual server configuration or paid certificates.

**Why this priority**: Core production milestone. The application cannot go live for client training or operational stores without a secure, reliable, automated HTTPS deployment workflow.

**Independent Test**: Execute `./deploy.sh` on an Ubuntu VPS with a configured domain name; verify all containers start, HTTPS certificate is automatically issued by Let's Encrypt, and the web app loads seamlessly in a browser with a valid padlock icon.

**Acceptance Scenarios**:
1. **Given** a server with Docker installed and DNS pointing to the server IP, **When** `./deploy.sh` is executed, **Then** all 4 containers (`database`, `backend`, `frontend`, `edge_proxy`) start and transition to healthy states within 60 seconds.
2. **Given** a user navigates to `http://domain.com`, **When** the request hits the edge proxy, **Then** it is automatically upgraded and redirected to `https://domain.com` with HTTP/2 or HTTP/3.

---

### User Story 2 - Zero-Downtime Automated Database Migrations (Priority: P1)

As a System Administrator, I want the backend service to automatically apply any pending Entity Framework Core database migrations upon container startup before traffic starts serving, so that database schema updates happen seamlessly without manual DB terminal access and without installing development SDKs on production machines.

**Why this priority**: Eliminates human error during releases, ensures database schema and code are always 100% synchronized, and guarantees database availability across deployments.

**Independent Test**: Spin up the backend container against a clean PostgreSQL instance; inspect logs to confirm the standalone migration bundle executes, applies all migration steps, and reports success before the HTTP listener opens on port 5000.

**Acceptance Scenarios**:
1. **Given** a newly deployed database with no existing tables, **When** the backend container launches, **Then** the migration bundle executes automatically, creates all operational tables, seeds system defaults, and transitions the readiness probe `/health/ready` to `Ready`.
2. **Given** database container is momentarily initializing, **When** the migration bundle runs, **Then** it retries up to 5 times with exponential backoff before reporting status, preventing container crash loops.

---

### User Story 3 - Zero-Trust Database Network Isolation (Priority: P2)

As a Security Officer, I want the production PostgreSQL database to be strictly confined to an isolated internal Docker bridge network with no host ports exposed to the public internet, so that direct unauthorized external connections to the database port 5432 are physically impossible.

**Why this priority**: Essential enterprise security compliance. Protects customer records, financial transactions, and store inventory from automated scanning bots and ransomware attacks.

**Independent Test**: Attempt to connect to port 5432 from an external machine or run `nmap` against the server IP; verify port 5432 is completely closed and unreachable, while the internal backend container communicates with `database:5432` without friction.

**Acceptance Scenarios**:
1. **Given** the production docker compose stack is running, **When** an external network scan probes port 5432 on the host IP, **Then** the connection is refused.
2. **Given** the backend container on `internal_backend_net`, **When** it queries `Host=database;Port=5432`, **Then** the connection succeeds with low latency.

---

### User Story 4 - Backward-Compatible API Versioning (Priority: P2)

As an API Consumer (Web Frontend, Future Mobile App, or B2B Integration), I want all backend endpoints to be canonically available under `/api/v1/...` while maintaining transparent fallback support for `/api/...`, so that current clients continue functioning with zero breakage and future major releases can introduce `/api/v2/...` independently.

**Why this priority**: Enables frictionless long-term software evolution. Allows new client versions to roll out progressively without stranding existing installations.

**Independent Test**: Issue HTTP GET/POST requests to `/api/v1/auth/login` and `/api/auth/login`; confirm both return identical valid HTTP 200 responses with consistent response schemas.

**Acceptance Scenarios**:
1. **Given** a request sent to `/api/v1/dashboard/summary`, **When** authenticated with a valid JWT, **Then** the system returns HTTP 200 with the versioned payload.
2. **Given** an older request sent to `/api/dashboard/summary`, **When** authenticated, **Then** the request is accepted and serviced identically without deprecation error.

---

### User Story 5 - Automated Backup and Disaster Recovery (Priority: P3)

As a Store Manager or System Owner, I want an automated scheduled backup process that creates daily compressed database archives at 03:00 AM, retains them for 14 days, and provides single-command restoration, so that operational business data is safeguarded against hardware failure or accidental deletion.

**Why this priority**: Business continuity guarantee. Required for enterprise-grade SaaS platforms and retail stores.

**Independent Test**: Trigger `./backup.sh`; verify a valid `.sql.gz` dump file is generated in `./backups/`. Verify that simulated files older than 14 days are automatically pruned.

**Acceptance Scenarios**:
1. **Given** active transactional data in the store, **When** `./backup.sh` is executed by cron, **Then** an intact, compressed `.sql.gz` relational backup is generated.
2. **Given** a blank database instance, **When** the decompressed backup file is piped into `psql`, **Then** the entire state is restored with 100% relational integrity.

---

### User Story 6 - Idempotent Production Master Seeding & First-Login Password Change (Priority: P3)

As a Store Owner logging in for the first time in production, I want the system to be populated with essential units and categories without fake transactions, and require me to set a secure personal password immediately upon first login, so that my production store is protected against default credential attacks.

**Why this priority**: Critical operational security. Eliminates vulnerability window from default temporary passwords.

**Independent Test**: Spin up a fresh database in production mode; log in with `owner@retailos.com`. Verify `mustChangePassword` is `true`, submit new password to `/api/v1/auth/change-password`, and confirm flag resets to `false`.

**Acceptance Scenarios**:
1. **Given** production environment startup, **When** seeding executes, **Then** only Units and the Owner account are provisioned with 0 fake products or sales.
2. **Given** initial owner login, **When** authenticated, **Then** the system flags `mustChangePassword: true` until the owner changes it.

---

## Edge Cases

- **What happens when the VPS reboots unexpectedly?**
  All services specify `restart: unless-stopped`, so Docker automatically restarts the database, backend, frontend, and reverse proxy in proper dependency order on system reboot.
- **What happens when Let's Encrypt rate limits or network issues occur?**
  Caddy automatically falls back to ZeroSSL or retries validation using ACME backoff without halting the internal HTTP service.
- **What happens if a migration fails during deployment?**
  The entrypoint script halts the backend startup and prevents the container from accepting incoming web traffic, leaving the previous stable state intact.
- **What happens if the frontend environment variables change?**
  Vite compiles `VITE_API_URL` during the multi-stage build; passing `args: VITE_API_URL=/api/v1` ensures relative pathing that works identically behind any domain or IP.
- **What happens when container log volumes grow indefinitely?**
  Docker log rotation caps each container's log file at 20MB with a maximum of 3 rotated archives, preventing host disk exhaustion.
- **What happens if the backup storage grows indefinitely?**
  `backup.sh` automatically prunes any `.sql.gz` backup archive older than 14 days upon every daily execution.
- **What happens if the container restarts multiple times in production?**
  `ProductionDataSeeder` runs idempotently by verifying whether `owner@retailos.com` or units already exist before executing any insert commands, preventing duplicate key violations.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a multi-stage `Dockerfile` for the .NET 9 Web API using `dotnet/sdk:9.0-alpine` for building and `aspnet:9.0-alpine` for runtime.
- **FR-002**: System MUST run the backend container under a non-root user (`USER $APP_UID`) for security compliance.
- **FR-003**: System MUST execute database schema migrations automatically on backend container startup using a self-contained EF Core migration bundle.
- **FR-004**: System MUST provide a multi-stage `Dockerfile` for the React/Vite frontend utilizing `node:22-alpine` for asset compilation and `nginx:alpine-slim` for static delivery.
- **FR-005**: The frontend web server MUST implement Single Page Application (SPA) routing fallback (`try_files $uri $uri/ /index.html;`).
- **FR-006**: The frontend web server MUST enable Gzip compression and enforce production security headers (`X-Frame-Options`, `Content-Security-Policy`, `X-Content-Type-Options`).
- **FR-007**: The orchestration stack (`docker-compose.prod.yml`) MUST define isolated network segmentation (`internal_backend_net` and `internal_frontend_net`).
- **FR-008**: The database service MUST NOT expose port 5432 to the host network or public internet.
- **FR-009**: The database service MUST persist all data files in a dedicated named volume (`pgdata`).
- **FR-010**: The edge reverse proxy MUST handle automatic TLS certificate provisioning and renewal via Let's Encrypt / ACME.
- **FR-011**: All backend controllers MUST support dual routing attributes: canonical `/api/v1/...` and backward-compatible `/api/...`.
- **FR-012**: System MUST provide an automated deployment bash script (`deploy.sh`) verifying prerequisites, building images, applying migrations, and checking container health.
- **FR-013**: The container orchestration stack MUST enforce Docker log rotation using the `json-file` logging driver with `max-size: 20m` and `max-file: 3` across all services, ensuring database and web server logs stream directly to standard streams (`stdout`/`stderr`) to prevent bounded volume bloat.
- **FR-014**: System MUST provide an automated database backup shell script (`backup.sh`) with cron installation instructions configured to execute daily at 03:00 AM, produce gzip-compressed SQL dumps (`.sql.gz`), and automatically prune backup archives older than 14 days to prevent disk exhaustion.
- **FR-015**: When `ASPNETCORE_ENVIRONMENT` is set to `Production`, system MUST seed only master foundation data (Store, default Owner account, core units, and general category) idempotently without fake invoices or dummy products, and enforce mandatory password change on first login via `MustChangePassword` claim and `POST /api/v1/auth/change-password` endpoint.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Total production deployment time from clean VPS to fully functioning HTTPS web application is under **5 minutes**.
- **SC-002**: Database security audit confirms **0 ports** open to the public internet on the database service.
- **SC-003**: Backend runtime container image size is under **200 MB** (achieved via .NET 9 Alpine multi-stage build).
- **SC-004**: Production health probe (`/health/live`) responds with HTTP 200 in under **50 milliseconds**.
- **SC-005**: 100% of existing API endpoints respond identically on both `/api/v1/...` and `/api/...` routes without regression.
- **SC-006**: Total Docker container log retention footprint is bounded to a maximum of **60 MB per container** on the host file system.
- **SC-007**: Automated database backup completes and produces a verified gzip-compressed archive in under **30 seconds** without interrupting active customer POS checkout transactions.
- **SC-008**: Production startup seeding executes idempotently in under **500 milliseconds**, verifying that multiple container restarts produce 0 duplicate key exceptions and maintain exactly 1 owner account with 0 fake transactions.

---

## Assumptions

- Target production host is a Linux server (Ubuntu 22.04 LTS or 24.04 LTS recommended) with Docker and Docker Compose plugin installed.
- The user has configured their domain DNS `A Record` to resolve to the public IP address of the server.
- Secret credentials (database passwords, JWT secret keys) are injected via `.env` and are never committed directly into source code repositories.
- Internal container communication uses service name resolution (`http://backend:5000`, `database:5432`, `http://frontend:80`).

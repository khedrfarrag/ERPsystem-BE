# Phase 1 Quickstart Validation Guide: Production Deployment (015-production-deployment)

This quickstart guide provides step-by-step instructions to validate the complete production deployment workflow on a Linux VPS or local Docker test environment.

---

## 1. Prerequisites Checklist

- [ ] Linux Ubuntu Server (22.04 or 24.04 LTS) with public IP
- [ ] Docker (v24+) and Docker Compose (v2.20+) installed
- [ ] Registered Domain Name with DNS `A Record` resolving to server IP
- [ ] Ports `80` (HTTP) and `443` (HTTPS) open in VPS firewall (UFW/Security Groups)

---

## 2. Deployment Execution Sequence

```bash
# 1. Clone or copy code to the server
git clone <YOUR_REPOSITORY_URL> /opt/retailos
cd /opt/retailos/system-BE

# 2. Configure production environment
cp .env.production.example .env

# Edit .env and supply your real values:
# DOMAIN=app.yourstore.com
# ACME_EMAIL=admin@yourstore.com
# POSTGRES_PASSWORD=<StrongPassword123!>
# JWT_SECRET=<GenerateWith: openssl rand -base64 48>
nano .env

# 3. Launch automated deployment
chmod +x deploy.sh entrypoint.sh
./deploy.sh
```

---

## 3. Post-Deployment Validation Scenarios

### Scenario A: Verify Container Health
```bash
docker compose -f docker-compose.prod.yml ps
```
**Expected Outcome**: All 4 services show status `Up (healthy)`.

---

### Scenario B: Verify Database Network Isolation
```bash
# From a remote computer outside the server:
nc -zv YOUR_SERVER_IP 5432
# or
nmap -p 5432 YOUR_SERVER_IP
```
**Expected Outcome**: Connection refused / Port 5432 closed or filtered. External traffic cannot reach the database directly.

---

### Scenario C: Verify API Versioning (v1 Canonical vs Legacy Fallback)
```bash
# Test v1 Canonical Endpoint
curl -s https://app.yourstore.com/api/v1/health/live

# Test Legacy Fallback Endpoint
curl -s https://app.yourstore.com/api/health/live
```
**Expected Outcome**: Both return HTTP 200 with `{ "status": "Live", ... }`.

---

### Scenario D: Verify Automated HTTPS & Certificate
```bash
curl -Iv https://app.yourstore.com/
```
**Expected Outcome**:
- Valid TLS handshake issued by Let's Encrypt / ZeroSSL.
- HTTP status 200 serving React SPA `index.html`.
- Security headers present: `Strict-Transport-Security`, `X-Frame-Options: SAMEORIGIN`.

---

### Scenario E: Disaster Recovery Backup & Restore
```bash
# 1. Take snapshot
docker compose -f docker-compose.prod.yml exec -T database pg_dump -U retailos_admin retailos_prod > test_backup.sql

# 2. Verify file size is non-zero
ls -lh test_backup.sql

# 3. Test restoration
cat test_backup.sql | docker compose -f docker-compose.prod.yml exec -T database psql -U retailos_admin retailos_prod
```
**Expected Outcome**: Backup file is generated and successfully re-applied with 0 SQL errors.

#!/bin/bash
set -euo pipefail

# ==============================================================================
# RetailOS Enterprise Deployment Automation Script (Ubuntu VPS)
# ==============================================================================

BOLD='\033[1m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${BOLD}${GREEN}====================================================================${NC}"
echo -e "${BOLD}${GREEN}        Starting RetailOS Enterprise Production Deployment          ${NC}"
echo -e "${BOLD}${GREEN}====================================================================${NC}"

# 1. Verify Prerequisites
command -v docker >/dev/null 2>&1 || { echo -e "${RED}[ERROR] Docker is not installed. Please install Docker first.${NC}" >&2; exit 1; }
docker compose version >/dev/null 2>&1 || { echo -e "${RED}[ERROR] Docker Compose plugin is not installed.${NC}" >&2; exit 1; }

# 2. Check for .env file and enforce strict security isolation
if [ -f ".env" ]; then
    chmod 600 .env
    echo -e "${GREEN}[Security] Enforced strict 600 permissions on .env (Owner read/write only).${NC}"
fi

if [ ! -f ".env" ]; then
    if [ -f ".env.production.example" ]; then
        echo -e "${YELLOW}[WARNING] .env not found. Creating from .env.production.example...${NC}"
        cp .env.production.example .env
        chmod 600 .env
        echo -e "${YELLOW}[ACTION REQUIRED] Please edit .env with your real domain and secrets before continuing!${NC}"
        exit 1
    else
        echo -e "${RED}[ERROR] .env file is missing.${NC}" >&2
        exit 1
    fi
fi

# 3. Pull latest code (if inside a git repository)
if [ -d ".git" ]; then
    echo -e "${GREEN}[1/5] Pulling latest repository commits...${NC}"
    git pull origin main || git pull origin master || echo -e "${YELLOW}Notice: git pull skipped.${NC}"
fi

# 4. Build Containers
echo -e "${GREEN}[2/5] Building production images (.NET 9 + React Nginx + Caddy)...${NC}"
docker compose -f docker-compose.prod.yml build

# 5. Start Database & Backend Services
echo -e "${GREEN}[3/5] Starting database and applying automated EF Core migrations...${NC}"
docker compose -f docker-compose.prod.yml up -d database
echo -e "Waiting for PostgreSQL database to pass healthcheck..."
docker compose -f docker-compose.prod.yml exec -T database sh -c 'until pg_isready; do sleep 1; done'

# 6. Launch Full Stack
echo -e "${GREEN}[4/5] Launching full production stack (Backend + Frontend + Edge Proxy)...${NC}"
docker compose -f docker-compose.prod.yml up -d --remove-orphans

# 7. Verify Health
echo -e "${GREEN}[5/5] Verifying system health and container statuses...${NC}"
sleep 5
docker compose -f docker-compose.prod.yml ps

echo -e "\n${BOLD}${GREEN}====================================================================${NC}"
echo -e "${BOLD}${GREEN}     RetailOS is now LIVE in Production with Automated HTTPS!       ${NC}"
echo -e "${BOLD}${GREEN}====================================================================${NC}"
echo -e "Live API Versioned Endpoint: https://${DOMAIN:-localhost}/api/v1"
echo -e "Live Web Application:        https://${DOMAIN:-localhost}"
echo -e "Database:                   PostgreSQL (Isolated internal network)"
echo -e "Health Probe:               https://${DOMAIN:-localhost}/health/live"
echo -e "====================================================================\n"

# Clean up dangling build images to save disk space
docker image prune -f >/dev/null 2>&1 || true

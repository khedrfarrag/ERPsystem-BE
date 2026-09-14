#!/bin/bash
set -euo pipefail

# ==============================================================================
# RetailOS Enterprise Automated Database Backup Script
# Scheduled Execution: Daily at 03:00 AM via Crontab
# Retention Policy: 14 Days Rolling Window (Older archives automatically pruned)
# ==============================================================================

BACKUP_DIR="${BACKUP_DIR:-./backups}"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILENAME="retailos_db_backup_${TIMESTAMP}.sql.gz"
TARGET_FILE="${BACKUP_DIR}/${BACKUP_FILENAME}"

# 1. Ensure backup directory exists with restricted permissions
mkdir -p "${BACKUP_DIR}"
chmod 700 "${BACKUP_DIR}"

echo "[$(date '+%Y-%m-%d %H:%M:%S')] Starting automated database backup..."

# 2. Extract database credentials from .env if present
DB_USER="retailos_admin"
DB_NAME="retailos_prod"

if [ -f ".env" ]; then
    DB_USER=$(grep -E '^POSTGRES_USER=' .env | cut -d '=' -f2- | tr -d '"' | tr -d "'" || echo "retailos_admin")
    DB_NAME=$(grep -E '^POSTGRES_DB=' .env | cut -d '=' -f2- | tr -d '"' | tr -d "'" || echo "retailos_prod")
fi

# 3. Stream pg_dump through gzip into timestamped backup file
if docker compose -f docker-compose.prod.yml exec -T database pg_dump -U "${DB_USER}" "${DB_NAME}" | gzip > "${TARGET_FILE}"; then
    BACKUP_SIZE=$(ls -lh "${TARGET_FILE}" | awk '{print $5}')
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] Backup successfully created: ${TARGET_FILE} (${BACKUP_SIZE})"
    chmod 600 "${TARGET_FILE}"
else
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] [ERROR] Database backup failed!" >&2
    exit 1
fi

# 4. Prune backup files older than 14 days
echo "[$(date '+%Y-%m-%d %H:%M:%S')] Pruning backup archives older than 14 days..."
find "${BACKUP_DIR}" -type f -name "retailos_db_backup_*.sql.gz" -mtime +14 -exec echo "Removing expired backup: {}" \; -delete

echo "[$(date '+%Y-%m-%d %H:%M:%S')] Backup procedure completed successfully."

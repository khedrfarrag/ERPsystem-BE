#!/bin/sh
set -e

echo "=========================================================="
echo " Starting RetailOS Enterprise Backend (.NET 9 Web API)    "
echo " Environment: ${ASPNETCORE_ENVIRONMENT:-Production}       "
echo "=========================================================="

# 1. Automatic Database Migration Execution
if [ -f "./bundle" ]; then
    echo "[RetailOS] Detected EF Core Migration Bundle. Applying migrations..."
    
    # Retry loop up to 5 times waiting for PostgreSQL to be fully ready
    MAX_RETRIES=5
    COUNT=0
    MIGRATED=0

    while [ $COUNT -lt $MAX_RETRIES ]; do
        COUNT=$((COUNT + 1))
        echo "[RetailOS] Migration attempt $COUNT of $MAX_RETRIES..."
        if ./bundle --connection "${ConnectionStrings__Default}"; then
            echo "[RetailOS] Database schema successfully updated to latest migration!"
            MIGRATED=1
            break
        else
            echo "[RetailOS] Database not ready yet or lock busy. Retrying in 4 seconds..."
            sleep 4
        fi
    done

    if [ $MIGRATED -eq 0 ]; then
        echo "[RetailOS - ERROR] Failed to apply database migrations after $MAX_RETRIES attempts. Exiting."
        exit 1
    fi
else
    echo "[RetailOS] No migration bundle executable found; skipping auto-migration."
fi

# 2. Start RetailOS.Api Web Service
echo "[RetailOS] Launching RetailOS.Api.dll on port 5000..."
exec dotnet RetailOS.Api.dll

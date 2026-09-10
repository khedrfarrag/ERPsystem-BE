# Quickstart Validation Guide: Backend Foundation (Phase 1)

**Feature**: 001-backend-foundation | **Date**: 2026-09-01

This guide describes how to validate that the foundation is correctly implemented,
end-to-end, without requiring manual browser testing. All scenarios are runnable
via automated integration tests or HTTP client (curl/Postman).

---

## Prerequisites

- .NET 8 SDK installed
- Docker Desktop running (for Testcontainers)
- PostgreSQL NOT required locally — Testcontainers spins it up automatically for tests
- For manual validation: a running instance with a `.env` or `appsettings.Development.json`
  configured (see Environment Setup below)

---

## Environment Setup (Manual Validation Only)

```json
// appsettings.Development.json (never commit secrets)
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=retailos_dev;Username=postgres;Password=dev"
  },
  "Jwt": {
    "Secret": "your-256-bit-secret-here",
    "Issuer": "RetailOS",
    "Audience": "RetailOS",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "RateLimiting": {
    "AuthWindowSeconds": 60,
    "AuthMaxRequests": 5
  }
}
```

Apply migrations:
```powershell
dotnet ef database update --project src/RetailOS.Infrastructure --startup-project src/RetailOS.Api
```

Run the API:
```powershell
dotnet run --project src/RetailOS.Api
```

---

## Scenario 1: Full Auth Flow

Validates FR-001 through FR-007, SC-001.

```powershell
# 1. Register a new store + owner
$register = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/register" `
  -Method POST -ContentType "application/json" `
  -Body '{"storeName":"Test Store","businessType":"Grocery","ownerFirstName":"Mostafa","ownerLastName":"Ahmed","email":"owner@test.com","password":"SecurePass123!"}'

$accessToken = $register.data.accessToken
$refreshToken = $register.data.refreshToken

# 2. Call /me with the access token
Invoke-RestMethod -Uri "http://localhost:5000/api/auth/me" `
  -Headers @{ Authorization = "Bearer $accessToken" }
# Expected: user profile with role=Owner and store details

# 3. Refresh the token
$refreshed = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/refresh" `
  -Method POST -ContentType "application/json" `
  -Body "{`"refreshToken`":`"$refreshToken`"}"
# Expected: new accessToken + new refreshToken

# 4. Use old refresh token again (should fail — single-use rotation)
Invoke-RestMethod -Uri "http://localhost:5000/api/auth/refresh" `
  -Method POST -ContentType "application/json" `
  -Body "{`"refreshToken`":`"$refreshToken`"}"
# Expected: 401 INVALID_REFRESH_TOKEN

# 5. Logout
Invoke-RestMethod -Uri "http://localhost:5000/api/auth/logout" `
  -Method POST -ContentType "application/json" `
  -Headers @{ Authorization = "Bearer $($refreshed.data.accessToken)" } `
  -Body "{`"refreshToken`":`"$($refreshed.data.refreshToken)`"}"
# Expected: 204 No Content
```

**Pass condition**: All steps complete without error; step 4 returns 401.

---

## Scenario 2: Tenant Isolation

Validates FR-008 through FR-010, SC-002.

```powershell
# Register Store A
$storeA = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/register" `
  -Method POST -ContentType "application/json" `
  -Body '{"storeName":"Store A","businessType":"Grocery","ownerFirstName":"Ali","ownerLastName":"A","email":"ali@storea.com","password":"Pass123!"}'

# Register Store B
$storeB = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/register" `
  -Method POST -ContentType "application/json" `
  -Body '{"storeName":"Store B","businessType":"Grocery","ownerFirstName":"Omar","ownerLastName":"B","email":"omar@storeb.com","password":"Pass123!"}'

# Get Store B's user ID from its own /me endpoint
$meBStoreB = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/me" `
  -Headers @{ Authorization = "Bearer $($storeB.data.accessToken)" }
$storeBUserId = $meBStoreB.data.id

# Try to access Store B's user from Store A's token
Invoke-RestMethod -Uri "http://localhost:5000/api/users/$storeBUserId" `
  -Headers @{ Authorization = "Bearer $($storeA.data.accessToken)" }
# Expected: 404 USER_NOT_FOUND (not 403, not Store B's data)
```

**Pass condition**: Store A token never returns Store B data — always 404.

---

## Scenario 3: Role-Based Access Control

Validates FR-011 through FR-014, SC-003.

```powershell
# Using Store A token from Scenario 2
$ownerToken = $storeA.data.accessToken

# Create a Cashier user
$cashier = Invoke-RestMethod -Uri "http://localhost:5000/api/users" `
  -Method POST -ContentType "application/json" `
  -Headers @{ Authorization = "Bearer $ownerToken" } `
  -Body '{"firstName":"Yara","lastName":"Cashier","email":"yara@storea.com","password":"Pass123!","role":"Cashier"}'

# Login as Cashier
$cashierLogin = Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" `
  -Method POST -ContentType "application/json" `
  -Body '{"email":"yara@storea.com","password":"Pass123!"}'
$cashierToken = $cashierLogin.data.accessToken

# Cashier tries to list users (Owner/Manager only) → should fail
Invoke-RestMethod -Uri "http://localhost:5000/api/users" `
  -Headers @{ Authorization = "Bearer $cashierToken" }
# Expected: 403 FORBIDDEN

# Cashier can call /me → should succeed
Invoke-RestMethod -Uri "http://localhost:5000/api/auth/me" `
  -Headers @{ Authorization = "Bearer $cashierToken" }
# Expected: 200 with Cashier profile
```

**Pass condition**: Cashier gets 403 on user list, 200 on /me.

---

## Scenario 4: Deactivated User Cannot Authenticate

Validates FR-017, SC-004.

```powershell
# Deactivate Cashier
$cashierId = $cashier.data.id
Invoke-RestMethod -Uri "http://localhost:5000/api/users/$cashierId/status" `
  -Method PATCH -ContentType "application/json" `
  -Headers @{ Authorization = "Bearer $ownerToken" } `
  -Body '{"isActive":false}'
# Expected: 200

# Deactivated Cashier tries to login
Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" `
  -Method POST -ContentType "application/json" `
  -Body '{"email":"yara@storea.com","password":"Pass123!"}'
# Expected: 403 ACCOUNT_DEACTIVATED
```

**Pass condition**: Deactivated user receives 403 on login attempt.

---

## Automated Test Suite

Run integration tests (Testcontainers handles PostgreSQL automatically):

```powershell
dotnet test tests/RetailOS.IntegrationTests --verbosity normal
```

Expected output:
```
Test Run Successful.
Total tests: [N]
     Passed: [N]
     Failed: 0
```

All 4 scenarios above must have corresponding integration test coverage.

---

## References

- API Contracts: [`contracts/auth.md`](./contracts/auth.md), [`contracts/users.md`](./contracts/users.md), [`contracts/stores.md`](./contracts/stores.md)
- Data Model: [`data-model.md`](./data-model.md)
- Research Decisions: [`research.md`](./research.md)
- Constitution: [`.specify/memory/constitution.md`](../../.specify/memory/constitution.md)

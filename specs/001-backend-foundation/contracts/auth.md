# API Contract: Authentication

**Feature**: 001-backend-foundation | **Version**: 1.0 | **Date**: 2026-09-01

All endpoints are prefixed with `/api/auth`. All responses follow the envelope:
```json
{ "success": true|false, "message": "...", "code": "...", "data": {...}, "errors": [] }
```

---

## POST /api/auth/register

Registers a new store and its Owner account in a single atomic operation.

**Rate Limit**: 3 requests per IP per minute.

**Request**:
```json
{
  "storeName": "Mostafa Detergents",
  "businessType": "Detergents",
  "ownerFirstName": "Mostafa",
  "ownerLastName": "Ahmed",
  "email": "mostafa@example.com",
  "password": "SecurePass123!"
}
```

**Response 201**:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "abc123...",
    "expiresIn": 900,
    "user": {
      "id": "uuid",
      "email": "mostafa@example.com",
      "firstName": "Mostafa",
      "lastName": "Ahmed",
      "role": "Owner",
      "storeId": "uuid",
      "storeName": "Mostafa Detergents"
    }
  }
}
```

**Errors**:
| Code | HTTP | Condition |
|---|---|---|
| `EMAIL_ALREADY_EXISTS` | 409 | Email already registered |
| `VALIDATION_ERROR` | 400 | Invalid input fields |

---

## POST /api/auth/login

Authenticates a user and issues access + refresh tokens.

**Rate Limit**: 5 requests per IP per minute (**MUST** per constitution).

**Request**:
```json
{
  "email": "mostafa@example.com",
  "password": "SecurePass123!"
}
```

**Response 200**:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "abc123...",
    "expiresIn": 900,
    "user": {
      "id": "uuid",
      "email": "mostafa@example.com",
      "firstName": "Mostafa",
      "lastName": "Ahmed",
      "role": "Owner",
      "storeId": "uuid",
      "storeName": "Mostafa Detergents"
    }
  }
}
```

**Errors**:
| Code | HTTP | Condition |
|---|---|---|
| `INVALID_CREDENTIALS` | 401 | Wrong email or password (never reveals which) |
| `ACCOUNT_DEACTIVATED` | 403 | User account is inactive |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many attempts |

---

## POST /api/auth/refresh

Issues a new access token using a valid refresh token. Rotates the refresh token (single-use).

**Rate Limit**: 5 requests per IP per minute (**MUST** per constitution).

**Request**:
```json
{
  "refreshToken": "abc123..."
}
```

**Response 200**:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "xyz789...",
    "expiresIn": 900
  }
}
```

**Errors**:
| Code | HTTP | Condition |
|---|---|---|
| `INVALID_REFRESH_TOKEN` | 401 | Token not found, expired, or already used |
| `ACCOUNT_DEACTIVATED` | 403 | User was deactivated after token was issued |

---

## POST /api/auth/logout

Revokes the current refresh token. Access token expires naturally (15 min).

**Auth**: Bearer token required.

**Request**:
```json
{
  "refreshToken": "abc123..."
}
```

**Response 204**: No content.

---

## GET /api/auth/me

Returns the authenticated user's profile and store context.

**Auth**: Bearer token required.

**Response 200**:
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "email": "mostafa@example.com",
    "firstName": "Mostafa",
    "lastName": "Ahmed",
    "role": "Owner",
    "store": {
      "id": "uuid",
      "name": "Mostafa Detergents",
      "businessType": "Detergents",
      "currency": "EGP",
      "timezone": "Africa/Cairo",
      "allowNegativeStock": false,
      "taxEnabled": false
    }
  }
}
```

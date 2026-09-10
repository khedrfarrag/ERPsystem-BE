# API Contract: Users

**Feature**: 001-backend-foundation | **Version**: 1.0 | **Date**: 2026-09-01

All endpoints are prefixed with `/api/users`. Bearer token required on all endpoints.
Responses follow the standard envelope format.

---

## GET /api/users

Lists all users in the authenticated user's store.

**Authorization**: Owner, Manager

**Query Parameters**:
| Param | Type | Default | Notes |
|---|---|---|---|
| `page` | int | 1 | Page number |
| `pageSize` | int | 25 | Max 100 |
| `isActive` | bool? | null | Filter by active status |
| `role` | string? | null | Filter by role name |

**Response 200**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "uuid",
        "email": "ahmed@example.com",
        "firstName": "Ahmed",
        "lastName": "Mohamed",
        "role": "Cashier",
        "isActive": true,
        "createdAt": "2026-09-01T00:00:00Z"
      }
    ],
    "totalCount": 5,
    "page": 1,
    "pageSize": 25
  }
}
```

---

## POST /api/users

Creates a new user in the authenticated user's store.

**Authorization**: Owner only

**Request**:
```json
{
  "firstName": "Ahmed",
  "lastName": "Mohamed",
  "email": "ahmed@example.com",
  "password": "TempPass123!",
  "role": "Cashier"
}
```

**Response 201**:
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "email": "ahmed@example.com",
    "firstName": "Ahmed",
    "lastName": "Mohamed",
    "role": "Cashier",
    "isActive": true,
    "storeId": "uuid"
  }
}
```

**Errors**:
| Code | HTTP | Condition |
|---|---|---|
| `EMAIL_ALREADY_EXISTS` | 409 | Email already in use globally |
| `INVALID_ROLE` | 400 | Role value is not valid |
| `FORBIDDEN` | 403 | Non-Owner attempting to create user |

---

## GET /api/users/{id}

Returns a single user. User must belong to the authenticated store.

**Authorization**: Owner, Manager

**Response 200**: Same shape as single item in the list above.

**Errors**:
| Code | HTTP | Condition |
|---|---|---|
| `USER_NOT_FOUND` | 404 | User not in this store or doesn't exist |

---

## PUT /api/users/{id}

Updates a user's details. Cannot change email via this endpoint.

**Authorization**: Owner only

**Request**:
```json
{
  "firstName": "Ahmed",
  "lastName": "Hassan",
  "role": "Manager"
}
```

**Response 200**: Updated user object.

---

## PATCH /api/users/{id}/status

Activates or deactivates a user account.

**Authorization**: Owner only

**Request**:
```json
{
  "isActive": false
}
```

**Response 200**:
```json
{
  "success": true,
  "data": { "id": "uuid", "isActive": false }
}
```

**Business Rules**:
- An Owner cannot deactivate their own account.
- Deactivated users are immediately unable to authenticate (refresh token also revoked).

**Errors**:
| Code | HTTP | Condition |
|---|---|---|
| `CANNOT_DEACTIVATE_SELF` | 400 | Owner trying to deactivate their own account |
| `USER_NOT_FOUND` | 404 | User not in this store |

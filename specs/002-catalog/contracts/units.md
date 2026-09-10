# API Contract: Units

**Base URL**: `/api/units`
**Authentication**: JWT Bearer (all endpoints)
**Tenant Scope**: All operations automatically scoped to the authenticated user's store

---

## Roles

| Endpoint | Required Role |
|---|---|
| GET (list, get by id) | Any authenticated role |
| POST / PUT / PATCH status | Owner, Manager |
| DELETE (soft) | Owner only |

---

## Endpoints

### GET /api/units
List units with optional filtering and pagination.

**Query Parameters**:
| Param | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Page number |
| `pageSize` | int | 25 | Items per page (max 100) |
| `isActive` | bool? | null | Filter by active status |

**Response 200**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "uuid",
        "name": "Kilogram",
        "symbol": "kg",
        "description": "Weight measurement",
        "isActive": true,
        "createdAt": "2026-09-01T00:00:00Z",
        "updatedAt": "2026-09-01T00:00:00Z"
      }
    ],
    "totalCount": 5,
    "page": 1,
    "pageSize": 25
  }
}
```

---

### GET /api/units/{id}
Get a single unit by ID.

**Response 200**: Single unit object.
**Response 404**: `{ "success": false, "code": "UNIT_NOT_FOUND", "message": "..." }`

---

### POST /api/units
Create a new unit.

**Request Body**:
```json
{
  "name": "Kilogram",
  "symbol": "kg",
  "description": "Weight measurement"
}
```

**Validation**:
- `name`: required, max 100 chars, unique within store (case-insensitive)
- `symbol`: required, max 20 chars, unique within store (case-insensitive)

**Response 201**: Created unit object.
**Response 409**: `{ "success": false, "code": "UNIT_NAME_CONFLICT" | "UNIT_SYMBOL_CONFLICT", "message": "..." }`

---

### PUT /api/units/{id}
Update a unit.

**Request Body**:
```json
{
  "name": "Kilogram",
  "symbol": "kg",
  "description": "Updated"
}
```

**Response 200**: Updated unit object.
**Response 404 / 409**: Not found / name or symbol conflict.

---

### PATCH /api/units/{id}/status
Activate or deactivate a unit.

**Request Body**: `{ "isActive": false }`
**Response 200**: Updated unit object.

---

### DELETE /api/units/{id}
Soft-delete a unit.

**Business Rule**: If the unit has any linked products, returns 409.

**Response 204**: Deleted.
**Response 409**: `{ "success": false, "code": "UNIT_HAS_PRODUCTS", "message": "Unit cannot be deleted while it has products assigned." }`

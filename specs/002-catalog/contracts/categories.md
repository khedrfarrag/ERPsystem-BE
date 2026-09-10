# API Contract: Categories

**Base URL**: `/api/categories`
**Authentication**: JWT Bearer (all endpoints)
**Tenant Scope**: All operations are automatically scoped to the authenticated user's store

---

## Roles

| Endpoint | Required Role |
|---|---|
| GET (list, get by id) | Any authenticated role |
| POST / PUT / PATCH status | Owner, Manager |
| DELETE (soft) | Owner only |

---

## Endpoints

### GET /api/categories
List categories with optional filtering and pagination.

**Query Parameters**:
| Param | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Page number |
| `pageSize` | int | 25 | Items per page (max 100) |
| `isActive` | bool? | null | Filter by active status |
| `search` | string? | null | Search by name (contains, case-insensitive) |

**Response 200**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "uuid",
        "name": "Beverages",
        "description": "All drink products",
        "isActive": true,
        "createdAt": "2026-09-01T00:00:00Z",
        "updatedAt": "2026-09-01T00:00:00Z"
      }
    ],
    "totalCount": 12,
    "page": 1,
    "pageSize": 25
  }
}
```

---

### GET /api/categories/{id}
Get a single category by ID.

**Response 200**: Single category object (same schema as list item).
**Response 404**: `{ "success": false, "code": "CATEGORY_NOT_FOUND", "message": "..." }`

---

### POST /api/categories
Create a new category.

**Request Body**:
```json
{
  "name": "Beverages",
  "description": "All drink products"
}
```

**Validation**:
- `name`: required, max 200 chars, unique within store (case-insensitive)

**Response 201**: Created category object.
**Response 400**: Validation errors.
**Response 409**: `{ "success": false, "code": "CATEGORY_NAME_CONFLICT", "message": "A category with this name already exists." }`

---

### PUT /api/categories/{id}
Update a category's name and description.

**Request Body**:
```json
{
  "name": "Drinks",
  "description": "Updated description"
}
```

**Response 200**: Updated category object.
**Response 404**: Category not found.
**Response 409**: Name conflict.

---

### PATCH /api/categories/{id}/status
Activate or deactivate a category.

**Request Body**:
```json
{ "isActive": false }
```

**Response 200**: Updated category object.
**Response 404**: Category not found.

---

### DELETE /api/categories/{id}
Soft-delete a category (sets `deleted_at`).

**Business Rule**: If the category has any linked products (even inactive ones), returns 409.

**Response 204**: No content (deleted).
**Response 409**: `{ "success": false, "code": "CATEGORY_HAS_PRODUCTS", "message": "Category cannot be deleted while it has products assigned." }`

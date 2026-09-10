# API Contract: Stores

**Feature**: 001-backend-foundation | **Version**: 1.0 | **Date**: 2026-09-01

All endpoints are prefixed with `/api/stores`. Bearer token required.

---

## GET /api/stores/current

Returns the authenticated user's store profile.

**Authorization**: Any authenticated role

**Response 200**:
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "name": "Mostafa Detergents",
    "businessType": "Detergents",
    "phone": "01012345678",
    "address": "Cairo, Egypt",
    "currency": "EGP",
    "timezone": "Africa/Cairo",
    "taxEnabled": false,
    "allowNegativeStock": false,
    "invoicePrefix": "INV",
    "isActive": true,
    "createdAt": "2026-09-01T00:00:00Z"
  }
}
```

---

## PUT /api/stores/current

Updates the current store's profile and settings.

**Authorization**: Owner only

**Request**:
```json
{
  "name": "Mostafa Detergents & Household",
  "phone": "01012345678",
  "address": "Cairo, Egypt",
  "taxEnabled": false,
  "allowNegativeStock": false,
  "invoicePrefix": "INV",
  "currency": "EGP",
  "timezone": "Africa/Cairo"
}
```

**Response 200**: Updated store object (same shape as GET).

**Business Rules**:
- `currency` and `timezone` changes take effect immediately for new records;
  historical records retain their original values (enforced at display layer).
- `allowNegativeStock` change takes effect on the next inventory operation.

**Errors**:
| Code | HTTP | Condition |
|---|---|---|
| `FORBIDDEN` | 403 | Non-Owner attempting to update store settings |
| `INVALID_CURRENCY` | 400 | Currency code not valid ISO 4217 |
| `INVALID_TIMEZONE` | 400 | Timezone not valid IANA identifier |

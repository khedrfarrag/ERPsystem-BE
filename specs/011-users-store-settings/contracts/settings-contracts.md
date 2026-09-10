# API & Component Contracts: Users Management & Store Settings

**Feature**: `011-users-store-settings`  
**Date**: 2026-09-09  
**Status**: Completed

---

## 1. REST API Endpoints

### 1.1 List Store Users
- **Endpoint**: `GET /api/users`
- **Auth**: Bearer JWT (`Owner`, `Manager`)
- **Query Parameters**:
  - `page` (int, default: 1)
  - `pageSize` (int, default: 20)
- **Response Format (`200 OK`)**:
```json
{
  "success": true,
  "message": null,
  "code": null,
  "data": {
    "items": [
      {
        "id": "01a07275-ec82-75ab-b622-1f5c1f614cd6",
        "email": "owner@retailos.com",
        "firstName": "محمود",
        "lastName": "المالك",
        "role": "Owner",
        "isActive": true,
        "storeId": "fde800f4-1734-4be5-a9b3-2c1b04a9a00c",
        "createdAt": "2026-09-01T12:00:00Z"
      },
      {
        "id": "12a07275-ec82-75ab-b622-1f5c1f614cd6",
        "email": "cashier@retailos.com",
        "firstName": "علي",
        "lastName": "الكاشير",
        "role": "Cashier",
        "isActive": true,
        "storeId": "fde800f4-1734-4be5-a9b3-2c1b04a9a00c",
        "createdAt": "2026-09-02T10:00:00Z"
      }
    ],
    "totalCount": 2,
    "page": 1,
    "pageSize": 20
  },
  "errors": null
}
```

---

### 1.2 Create New Store User
- **Endpoint**: `POST /api/users`
- **Auth**: Bearer JWT (`Owner`, `Manager`)
- **Request Body**:
```json
{
  "firstName": "سارة",
  "lastName": "أحمد",
  "email": "sara.cashier@retailos.com",
  "password": "Pass123456!",
  "role": "Cashier"
}
```
- **Response Format (`201 Created`)**:
  Returns `UserResponse` with generated ID.

---

### 1.3 Toggle User Active Status
- **Endpoint**: `PATCH /api/users/{id:guid}/status`
- **Auth**: Bearer JWT (`Owner`, `Manager`)
- **Request Body**:
```json
{
  "isActive": false
}
```
- **Response Format (`200 OK`)**:
  Returns updated `UserResponse`.

---

### 1.4 Update User Details & Role
- **Endpoint**: `PUT /api/users/{id:guid}`
- **Auth**: Bearer JWT (`Owner`, `Manager`)
- **Request Body**:
```json
{
  "firstName": "سارة",
  "lastName": "محمود",
  "role": "Manager"
}
```
- **Response Format (`200 OK`)**:
  Returns updated `UserResponse`.

---

### 1.5 Get Current Store Settings
- **Endpoint**: `GET /api/stores/current`
- **Auth**: Bearer JWT (`Owner`, `Manager`)
- **Response Format (`200 OK`)**:
```json
{
  "success": true,
  "message": null,
  "code": null,
  "data": {
    "id": "fde800f4-1734-4be5-a9b3-2c1b04a9a00c",
    "name": "مؤسسة الأمل للمنظفات والكيماويات",
    "businessType": "RetailStore",
    "phone": "01099887766",
    "address": "شارع الجمهورية، المنصورة، مصر",
    "currency": "EGP",
    "timezone": "Africa/Cairo",
    "taxEnabled": true,
    "allowNegativeStock": false,
    "invoicePrefix": "INV-",
    "isActive": true,
    "createdAt": "2026-09-01T12:00:00Z"
  },
  "errors": null
}
```

---

### 1.6 Update Store Configuration
- **Endpoint**: `PUT /api/stores/current`
- **Auth**: Bearer JWT (`Owner` only)
- **Request Body**:
```json
{
  "name": "مؤسسة الأمل للمنظفات والكيماويات",
  "phone": "01099887766",
  "address": "شارع الجمهورية، المنصورة، مصر",
  "taxEnabled": true,
  "allowNegativeStock": false,
  "invoicePrefix": "INV-",
  "currency": "EGP",
  "timezone": "Africa/Cairo"
}
```
- **Response Format (`200 OK`)**:
  Returns updated `StoreResponse`.

---

## 2. Frontend Component Architecture

```text
src/features/settings/
├── api/
│   └── settingsApi.ts           # Axios requests for users and stores
├── types/
│   └── settings.types.ts        # Interfaces and DTO definitions
├── hooks/
│   ├── useUsers.ts              # Users query, filter, create, status toggle hook
│   └── useStoreProfile.ts       # Store profile query and mutation hook
└── components/
    ├── UsersTab.tsx             # Staff table, status toggle, search bar
    ├── CreateUserModal.tsx      # Modal form to register new cashier/manager
    ├── EditUserModal.tsx        # Modal form to edit user name & role
    └── StoreProfileTab.tsx      # Store settings form with toggles & save
```

# API Contracts: B2B Wholesale Portal & Notifications

**Feature**: `012-b2b-wholesale-portal`  
**Date**: 2026-09-09  
**Status**: Completed

---

## 1. Merchant Management Endpoints (`/api/merchants`)
*Accessible by: `Owner`, `Manager`*

### `GET /api/merchants`
Returns paginated list of registered wholesale merchants.
- **Query Params**: `page=1`, `pageSize=20`, `search=string`
- **Response**: `200 OK`
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "tradeName": "محل الأمل للتجزئة",
      "contactPerson": "أحمد محمود",
      "phone": "+201012345678",
      "email": "alamal@example.com",
      "address": "القاهرة، شارع التحرير",
      "creditLimit": 50000.00,
      "currentBalance": 12500.00,
      "paymentTerms": "Net 15",
      "isActive": true,
      "createdAt": "2026-09-09T20:00:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

### `POST /api/merchants`
Registers a new wholesale merchant and creates linked portal login user.
- **Request Body**:
```json
{
  "tradeName": "محل الأمل للتجزئة",
  "contactPerson": "أحمد محمود",
  "phone": "+201012345678",
  "email": "alamal@example.com",
  "password": "Password123!",
  "address": "القاهرة، شارع التحرير",
  "creditLimit": 50000.00,
  "paymentTerms": "Net 15"
}
```
- **Response**: `201 Created`

---

## 2. B2B Orders Endpoints (`/api/b2b-orders`)

### `POST /api/b2b-orders`
*Accessible by: `Merchant`*
Places a new wholesale supply order.
- **Request Body**:
```json
{
  "paymentPreference": "Partial",
  "expectedDownPayment": 1500.00,
  "notes": "يرجى التوصيل قبل الساعة 2 ظهراً",
  "items": [
    {
      "productId": "4bb65718-d996-4191-bb2d-7bb0b81c2d0f",
      "quantity": 10
    }
  ]
}
```
- **Response**: `201 Created`

### `GET /api/b2b-orders`
*Accessible by: `Owner`, `Manager`, `Merchant` (filtered to self)*
Returns list of B2B orders.
- **Query Params**: `page=1`, `pageSize=20`, `status=Pending|Approved|Invoiced|Rejected`

### `POST /api/b2b-orders/{id}/approve`
*Accessible by: `Owner`, `Manager`*
Approves order with explicit approved quantities per line item.
- **Request Body**:
```json
{
  "items": [
    {
      "orderItemId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "approvedQuantity": 30.00,
      "adjustmentReason": "المتاح بالمخزن 30 فقط"
    }
  ]
}
```
- **Validation**:
  - `0 <= approvedQuantity <= min(requestedQuantity, availableStock)`.
  - `adjustmentReason` required if `approvedQuantity < requestedQuantity`.
  - Rejects if all items approved at 0.

### `POST /api/b2b-orders/{id}/invoice`
*Accessible by: `Owner`, `Manager`*
Converts approved order into a formal sales invoice and records inventory/ledger updates.
- **Request Body**:
```json
{
  "paidAmount": 2000.00,
  "discountAmount": 100.00,
  "paymentMethod": "Partial",
  "creditLimitOverrideConfirmed": true,
  "creditLimitOverrideReason": "تاجر منتظم وتمت موافقة الإدارة على تجاوز مؤقت",
  "notes": "تم الاستلام والتسليم"
}
```
- **Error Responses**:
  - `409 Conflict` (Code: `CREDIT_LIMIT_EXCEEDED`): Returned if `projectedBalance > creditLimit` and override was not confirmed. Payload contains calculated financial figures for admin display.
  - `409 Conflict` (Code: `STOCK_CHANGED_SINCE_APPROVAL`): Returned if physical warehouse stock dropped below approved quantities prior to invoicing.
  - `400 Bad Request`: Returned if override confirmed but reason is missing/too short (<10 chars).

### `POST /api/b2b-orders/{id}/reject`
*Accessible by: `Owner`, `Manager`*
Rejects the order with an explanatory reason.
- **Request Body**:
```json
{
  "rejectionReason": "نقص كميات الصنف وعدم توفر بديل حالياً"
}
```

### `POST /api/b2b-orders/{id}/cancel`
*Accessible by: `Merchant` (for own order only)*
Cancels a pending B2B order. Returns `409 Conflict` (Code: `ORDER_NOT_CANCELLABLE`) if status is not `Pending`.
- **Request Body**:
```json
{
  "reason": "تم الطلب بالخطأ"
}
```
- **Response**: `200 OK` (Returns updated order object with `status: "Cancelled"`, `cancelledAt`, and `cancellationReason`).

---

## 3. Notifications Endpoints (`/api/notifications`)
*Accessible by: `Owner`, `Manager`*

### `GET /api/notifications`
Returns recent notifications list.
- **Query Params**: `limit=20`, `unreadOnly=false`

### `GET /api/notifications/unread-count`
Returns count of unread notifications for badge display.
- **Response**: `200 OK`
```json
{
  "unreadCount": 3
}
```

### `POST /api/notifications/{id}/mark-read`
Marks single notification as read.

### `POST /api/notifications/mark-all-read`
Marks all active notifications as read.

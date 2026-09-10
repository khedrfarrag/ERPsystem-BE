# API Contracts & Interface Schemas: Operations Hardening

**Feature**: `013-operations-fixes-hardening`  
**Date**: 2026-09-10  

---

## 1. Cash Register Shift Close Endpoint
`POST /api/cash-register/close`

### Request Body:
```json
{
  "countedAmount": 1480.00,
  "notes": "إغلاق الوردية الصباحية - تم توريد النقدية للخزينة"
}
```

### Response (HTTP 200):
```json
{
  "success": true,
  "data": {
    "expectedBalance": 1500.00,
    "countedAmount": 1480.00,
    "discrepancy": -20.00,
    "sweepAmount": 1480.00,
    "notes": "إغلاق الوردية الصباحية - تم توريد النقدية للخزينة",
    "closedAt": "2026-09-10T19:30:00Z"
  },
  "message": "تم إغلاق الوردية وتسوية وتصفير الدرج بنجاح."
}
```

---

## 2. B2B Order Approval Endpoint (With Cost Safeguard)
`POST /api/b2b-orders/{id}/approve`

### Request Body:
```json
{
  "items": [
    {
      "orderItemId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "approvedQuantity": 10,
      "unitWholesalePrice": 45.00,
      "adjustmentReason": "تعديل سعر بناء على حجم الطلب"
    }
  ],
  "notes": "تم اعتماد الكمية"
}
```

### Error Response (HTTP 400 when unitWholesalePrice < purchaseCost 50.00):
```json
{
  "success": false,
  "code": "PRICE_BELOW_COST",
  "message": "لا يمكن بيع الصنف 'مسحوق غسيل 5 كجم' بسعر (45.00 ج.م) وهو أقل من سعر التكلفة (50.00 ج.م). لا يُسمح بالبيع بأقل من التكلفة."
}
```

---

## 3. Store Current Profile Update Endpoint (With Auto-Save)
`PUT /api/stores/current`

### Request Body:
```json
{
  "name": "مؤسسة الأمل للمنظفات والكيماويات",
  "phone": "01000000000",
  "address": "القاهرة - مصر",
  "taxEnabled": true,
  "allowNegativeStock": false,
  "invoicePrefix": "INV-",
  "currency": "EGP",
  "timezone": "Africa/Cairo"
}
```

### Response (HTTP 200):
```json
{
  "success": true,
  "data": {
    "id": "fde800f4-1734-4be5-a9b3-2c1b04a9a00c",
    "name": "مؤسسة الأمل للمنظفات والكيماويات",
    "taxEnabled": true,
    "allowNegativeStock": false
  },
  "message": "Store settings updated successfully."
}
```

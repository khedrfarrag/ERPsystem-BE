# API Contracts: AI Invoice Scanner

**Controller**: `AiInvoiceController`  
**Route Prefix**: `/api/ai/invoices`  
**Authorization**: `[Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]`  

---

## 1. Scan Invoice (Upload & Extract)

- **Endpoint**: `POST /api/ai/invoices/scan`
- **Consumes**: `multipart/form-data`
- **Request Limits**: Max 10MB
- **Parameters**:
  - `file`: `IFormFile` (Supported: `.jpg`, `.jpeg`, `.png`, `.webp`, `.pdf`)

### Response: `200 OK`
```json
{
  "success": true,
  "message": "تم تحليل الفاتورة بنجاح",
  "code": null,
  "data": {
    "supplierName": "شركة الأهرام للتوزيع",
    "matchedSupplierId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "invoiceNumber": "INV-2026-9041",
    "invoiceDate": "2026-09-12T00:00:00Z",
    "totalAmount": 1450.00,
    "taxAmount": 0.00,
    "discountAmount": 50.00,
    "items": [
      {
        "lineNumber": 1,
        "rawItemName": "صابون ديتول سائل 500 مل",
        "barcode": "6221234567890",
        "matchedProductId": "01a07275-ec82-75ab-b622-1f5c1f614cd6",
        "matchedProductName": "صابون سائل ديتول 500 مل",
        "isNewProduct": false,
        "confidenceScore": 0.98,
        "categoryName": "المنظفات",
        "unitSymbol": "قطعة",
        "quantity": 24,
        "unitCost": 25.00,
        "suggestedSellingPrice": 35.00,
        "subTotal": 600.00
      },
      {
        "lineNumber": 2,
        "rawItemName": "منظف أرضيات لافندر 3 لتر",
        "barcode": null,
        "matchedProductId": null,
        "matchedProductName": null,
        "isNewProduct": true,
        "confidenceScore": 1.0,
        "categoryName": "المنظفات",
        "unitSymbol": "لتر",
        "quantity": 10,
        "unitCost": 85.00,
        "suggestedSellingPrice": 110.00,
        "subTotal": 850.00
      }
    ]
  },
  "errors": []
}
```

### Error Responses
- `400 Bad Request`: `{ "success": false, "message": "صيغة الملف غير مدعومة أو الصورة غير واضحة", "code": "INVALID_INVOICE_IMAGE" }`
- `401 Unauthorized`: Token missing or invalid.
- `403 Forbidden`: User does not possess `Owner` or `Manager` role.
- `503 Service Unavailable`: `{ "success": false, "message": "خدمة الذكاء الاصطناعي غير متوفرة حالياً أو تم تجاوز حد الطلبات", "code": "AI_SERVICE_UNAVAILABLE" }`

---

## 2. Commit Invoice (Apply to Purchases or Catalog)

- **Endpoint**: `POST /api/ai/invoices/commit`
- **Consumes**: `application/json`
- **Body**: `CommitAiInvoiceRequest`

### Request Payload:
```json
{
  "mode": "Purchase",
  "supplierName": "شركة الأهرام للتوزيع",
  "supplierId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "invoiceNumber": "INV-2026-9041",
  "invoiceDate": "2026-09-12T00:00:00Z",
  "notes": "تم الاستيراد عبر المسح الذكي بالذكاء الاصطناعي",
  "items": [
    {
      "productId": "01a07275-ec82-75ab-b622-1f5c1f614cd6",
      "name": "صابون سائل ديتول 500 مل",
      "barcode": "6221234567890",
      "categoryName": "المنظفات",
      "unitSymbol": "قطعة",
      "quantity": 24,
      "unitCost": 25.00,
      "sellingPrice": 35.00
    },
    {
      "productId": null,
      "name": "منظف أرضيات لافندر 3 لتر",
      "barcode": null,
      "categoryName": "المنظفات",
      "unitSymbol": "لتر",
      "quantity": 10,
      "unitCost": 85.00,
      "sellingPrice": 110.00
    }
  ]
}
```

### Response: `200 OK`
```json
{
  "success": true,
  "message": "تم اعتماد الفاتورة وتحديث المخزون بنجاح",
  "code": null,
  "data": {
    "success": true,
    "message": "تم اعتماد الفاتورة وتحديث المخزون بنجاح",
    "purchaseId": "5fa85f64-5717-4562-b3fc-2c963f66b9f1",
    "purchaseNumber": "PUR-20260912-004",
    "productsCreated": 1,
    "productsUpdated": 1,
    "inventoryItemsIncreased": 2,
    "totalProcessedAmount": 1450.00
  },
  "errors": []
}
```

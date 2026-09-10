# API & Component Contracts: Sales Invoices History & Returns

**Feature**: `010-sales-history-returns`  
**Date**: 2026-09-06  
**Status**: Completed

---

## 1. REST API Endpoints

### 1.1 List Sales Invoices
- **Endpoint**: `GET /api/sales`
- **Auth**: Bearer JWT (`Owner`, `Manager`, `Cashier`)
- **Query Parameters**:
  - `customerId` (optional, UUID): Filter by customer.
  - `paymentMethod` (optional, string: `"Cash" | "Credit" | "Mixed"`): Filter by payment method.
  - `pageNumber` (int, default: 1): 1-based page index.
  - `pageSize` (int, default: 20, max: 100): Page size.
- **Response Format (`200 OK`)**:
```json
{
  "success": true,
  "message": null,
  "code": null,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "customerId": "8ba12f34-1111-4562-b3fc-2c963f66afa6",
        "customerName": "سوبر ماركت الأمل",
        "invoiceNumber": "INV-20260906-0012",
        "saleDate": "2026-09-06T14:22:00Z",
        "status": "Completed",
        "paymentMethod": "Cash",
        "subTotal": 450.00,
        "discountAmount": 0.00,
        "taxAmount": 63.00,
        "totalAmount": 513.00,
        "cashAmount": 513.00,
        "creditAmount": 0.00,
        "totalCost": 320.00,
        "notes": null,
        "createdAt": "2026-09-06T14:22:00Z",
        "items": [
          {
            "id": "1fa85f64-5717-4562-b3fc-2c963f66afa6",
            "productId": "4fa85f64-5717-4562-b3fc-2c963f66afa6",
            "productName": "زيت عافية 1.5 لتر",
            "quantity": 3.0,
            "unitPrice": 150.00,
            "unitCost": 106.66,
            "discount": 0.0,
            "subTotal": 450.00,
            "totalCost": 320.00
          }
        ]
      }
    ],
    "totalCount": 48,
    "pageNumber": 1,
    "pageSize": 20
  },
  "errors": null
}
```

---

### 1.2 Get Sale Invoice Details
- **Endpoint**: `GET /api/sales/{id:guid}`
- **Auth**: Bearer JWT (`Owner`, `Manager`, `Cashier`)
- **Response Format (`200 OK`)**:
  Returns `SaleResponse` matching the structure above, with full line items and returns history.

---

### 1.3 Process Sale Return
- **Endpoint**: `POST /api/sales/{id:guid}/returns`
- **Auth**: Bearer JWT (`Owner`, `Manager`)
- **Request Body**:
```json
{
  "reason": "منتج معيب تالف العبوة",
  "refundMethod": "Cash",
  "items": [
    {
      "productId": "4fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 1.0
    }
  ]
}
```
- **Response Format (`201 Created`)**:
```json
{
  "success": true,
  "message": "Sale return processed successfully.",
  "code": null,
  "data": {
    "id": "9fa85f64-5717-4562-b3fc-2c963f66afa6",
    "saleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "returnNumber": "SR-20260906-A1B2C3D4",
    "returnDate": "2026-09-06T15:00:00Z",
    "totalAmount": 150.00,
    "totalCost": 106.66,
    "refundMethod": "Cash",
    "reason": "منتج معيب تالف العبوة",
    "items": [
      {
        "id": "8fa85f64-5717-4562-b3fc-2c963f66afa6",
        "productId": "4fa85f64-5717-4562-b3fc-2c963f66afa6",
        "productName": "زيت عافية 1.5 لتر",
        "quantity": 1.0,
        "unitPrice": 150.00,
        "unitCost": 106.66,
        "subTotal": 150.00,
        "totalCost": 106.66
      }
    ]
  },
  "errors": null
}
```

---

## 2. Frontend Component Architecture

```
src/features/sales/
├── api/
│   └── salesApi.ts                  # Axios requests (getSales, getSaleById, createSaleReturn)
├── types/
│   └── sales.types.ts               # Interfaces & DTO definitions
├── hooks/
│   └── useSales.ts                  # Query, filter, pagination, and return dispatching hook
└── components/
    ├── SalesFilters.tsx             # Search input, customer dropdown, payment method pills
    ├── SalesTable.tsx               # High-contrast table with WCAG AA styling, status badges
    ├── SaleDetailsModal.tsx         # Full invoice modal, breakdown, reprint trigger
    ├── SaleReturnModal.tsx          # Return wizard with quantity inputs and reason selector
    └── ThermalReceiptPrint.tsx      # Hidden @media print receipt matching 80mm POS format
```

---

## 3. UI Component Props & Contracts

### `SalesTableProps`
```typescript
interface SalesTableProps {
  sales: Sale[];
  isLoading: boolean;
  onSelectSale: (sale: Sale) => void;
  onReprintSale: (sale: Sale) => void;
}
```

### `SaleDetailsModalProps`
```typescript
interface SaleDetailsModalProps {
  sale: Sale | null;
  isOpen: boolean;
  onClose: () => void;
  onReprint: () => void;
  onOpenReturn: () => void;
  canProcessReturn: boolean;
}
```

### `SaleReturnModalProps`
```typescript
interface SaleReturnModalProps {
  sale: Sale | null;
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (returnResponse: SaleReturn) => void;
}
```

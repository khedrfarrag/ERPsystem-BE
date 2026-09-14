# Data Model: AI Invoice Scanner

**Feature**: AI Invoice Scanner  
**Directory**: `specs/014-ai-invoice-scanner`  
**Date**: 2026-09-12  

---

## 1. Existing Reused Domain Entities

The feature leverages existing domain entities without modifying their schema (Zero Breaking Changes):

- **`Product`** (`src/RetailOS.Domain/Entities/Product.cs`):
  - `Id`, `StoreId`, `CategoryId`, `UnitId`, `Name`, `Barcode`, `PurchaseCost`, `SellingPrice`, `MinStockLevel`, `IsActive`.
- **`Purchase`** (`src/RetailOS.Domain/Entities/Purchase.cs`):
  - `Id`, `StoreId`, `SupplierId`, `PurchaseNumber`, `InvoiceNumber`, `PurchaseDate`, `Status`, `TotalAmount`, `Notes`, `InvoiceImageUrl`, `CreatedBy`.
- **`Store`** (`src/RetailOS.Domain/Entities/Store.cs`):
  - `Id`, `Name`, `EnableInvoiceArchiving` (bool, default `true`), other settings.
- **`PurchaseLineItem`** (`src/RetailOS.Domain/Entities/PurchaseLineItem.cs`):
  - `Id`, `StoreId`, `PurchaseId`, `ProductId`, `Quantity`, `UnitCost`, `Discount`, `SubTotal`.
- **`Supplier`** (`src/RetailOS.Domain/Entities/Supplier.cs`):
  - `Id`, `StoreId`, `Name`, `Phone`, `IsActive`.
- **`InventoryTransaction`** (`src/RetailOS.Domain/Entities/InventoryTransaction.cs`):
  - `Id`, `StoreId`, `ProductId`, `MovementType = MovementType.In`, `Reason = "PURCHASE"`, `Quantity`, `ReferenceId`.

---

## 2. New Application Layer DTOs & Contracts

Located in `src/RetailOS.Application/Ai/DTOs/`:

### `InvoiceScanPreviewDto`
```csharp
public record InvoiceScanPreviewDto(
    string? SupplierName,
    Guid? MatchedSupplierId,
    string? InvoiceNumber,
    DateTimeOffset? InvoiceDate,
    decimal TotalAmount,
    decimal? TaxAmount,
    decimal? DiscountAmount,
    DuplicateInvoiceWarningDto? DuplicateWarning,
    IReadOnlyList<InvoiceScanLineItemDto> Items
);

public record DuplicateInvoiceWarningDto(
    bool IsDuplicate,
    Guid ExistingPurchaseId,
    string ExistingPurchaseNumber,
    DateTimeOffset ExistingPurchaseDate,
    decimal ExistingTotalAmount,
    bool IsIdentical,
    string WarningMessage
);
```

### `InvoiceScanLineItemDto`
```csharp
public record InvoiceScanLineItemDto(
    int LineNumber,
    string RawItemName,
    string? Barcode,
    Guid? MatchedProductId,
    string? MatchedProductName,
    bool IsNewProduct,
    decimal ConfidenceScore,
    string CategoryName,
    string UnitSymbol,
    decimal Quantity,
    decimal UnitCost,
    decimal? SuggestedSellingPrice,
    decimal SubTotal
);
```

### `CommitAiInvoiceRequest`
```csharp
public record CommitAiInvoiceRequest(
    string Mode, // "Purchase" or "CatalogOnly"
    string? SupplierName,
    Guid? SupplierId,
    string? InvoiceNumber,
    DateTimeOffset? InvoiceDate,
    string? Notes,
    string? ImageTempKey,
    bool? SaveToArchive,
    bool AllowDuplicateOverride = false,
    IReadOnlyList<CommitAiInvoiceItemDto> Items = null!
);

public record CommitAiInvoiceItemDto(
    Guid? ProductId,
    string Name,
    string? Barcode,
    string CategoryName,
    string UnitSymbol,
    decimal Quantity,
    decimal UnitCost,
    decimal SellingPrice
);
```

### `CommitAiInvoiceResponse`
```csharp
public record CommitAiInvoiceResponse(
    bool Success,
    string Message,
    Guid? PurchaseId,
    string? PurchaseNumber,
    int ProductsCreated,
    int ProductsUpdated,
    int InventoryItemsIncreased,
    decimal TotalProcessedAmount
);
```

---

## 3. Gemini Extraction Schema (JSON Schema for Gemini API)

```json
{
  "type": "OBJECT",
  "properties": {
    "supplierName": { "type": "STRING" },
    "invoiceNumber": { "type": "STRING" },
    "invoiceDate": { "type": "STRING" },
    "totalAmount": { "type": "NUMBER" },
    "taxAmount": { "type": "NUMBER" },
    "discountAmount": { "type": "NUMBER" },
    "items": {
      "type": "ARRAY",
      "items": {
        "type": "OBJECT",
        "properties": {
          "name": { "type": "STRING" },
          "barcode": { "type": "STRING" },
          "category": { "type": "STRING" },
          "unit": { "type": "STRING" },
          "quantity": { "type": "NUMBER" },
          "unitCost": { "type": "NUMBER" },
          "total": { "type": "NUMBER" }
        },
        "required": ["name", "quantity", "unitCost", "total"]
      }
    }
  },
  "required": ["items", "totalAmount"]
}
```

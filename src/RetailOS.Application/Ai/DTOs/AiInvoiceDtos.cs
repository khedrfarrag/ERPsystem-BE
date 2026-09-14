namespace RetailOS.Application.Ai.DTOs;

public record InvoiceScanPreviewDto(
    string? SupplierName,
    Guid? MatchedSupplierId,
    string? InvoiceNumber,
    DateTimeOffset? InvoiceDate,
    decimal TotalAmount,
    decimal? TaxAmount,
    decimal? DiscountAmount,
    string? ImageTempKey,
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
    decimal? SellingPrice,
    decimal SubTotal,
    bool IsCostMissingOrZero = false,
    bool IsSellingBelowCost = false
);

public record CommitAiInvoiceRequest(
    string Mode, // "Purchase" or "CatalogOnly"
    string? SupplierName,
    Guid? SupplierId,
    string? InvoiceNumber,
    DateTimeOffset? InvoiceDate,
    string? Notes,
    string? ImageTempKey,
    bool? SaveToArchive,
    bool AllowDuplicateOverride,
    IReadOnlyList<CommitAiInvoiceItemDto> Items
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

public record CommitAiInvoiceResponse(
    bool Success,
    string Message,
    Guid? PurchaseId,
    string? PurchaseNumber,
    string? InvoiceImageUrl,
    int ProductsCreated,
    int ProductsUpdated,
    int InventoryItemsIncreased,
    decimal TotalProcessedAmount
);

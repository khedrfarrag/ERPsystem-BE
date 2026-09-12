namespace RetailOS.Application.Products.DTOs;

public record ImportRowError(
    int Row,
    string? Field,
    string Message
);

public record ImportRowPreviewDto(
    int RowNumber,
    string Name,
    string? Barcode,
    string CategoryName,
    string UnitSymbol,
    decimal SellingPrice,
    decimal? PurchaseCost,
    bool IsValid,
    IReadOnlyList<string> Errors
);

public record ImportPreviewResponse(
    int TotalRows,
    int ValidRows,
    int ErrorRows,
    IReadOnlyList<ImportRowError> Errors,
    IReadOnlyList<ImportRowPreviewDto>? Rows = null,
    int? ValidRowsCount = null,
    int? InvalidRowsCount = null
);

public record ImportCommitResponse(
    int TotalProcessed,
    int Created,
    int SkippedDuplicate,
    int SkippedInvalid,
    int? ImportedCount = null,
    int? SkippedCount = null
);

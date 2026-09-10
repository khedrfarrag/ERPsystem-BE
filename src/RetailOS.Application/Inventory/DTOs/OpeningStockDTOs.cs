namespace RetailOS.Application.Inventory.DTOs;

public record RecordOpeningStockRequest(
    Guid ProductId,
    decimal Quantity,
    decimal CostPerUnit
);

public record OpeningStockEntryRequest(
    Guid ProductId,
    decimal Quantity,
    decimal CostPerUnit
);

public record BulkOpeningStockRequest(
    IReadOnlyList<OpeningStockEntryRequest> Entries
);

public record OpeningStockResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal CostPerUnit,
    decimal TotalValue,
    string Reason,
    DateTime CreatedAt
);

public record BulkOpeningStockEntryError(
    Guid ProductId,
    string Code,
    string Message
);

public record BulkOpeningStockResponse(
    int TotalRequested,
    int Created,
    int SkippedAlreadyExists,
    int SkippedInvalid,
    IReadOnlyList<BulkOpeningStockEntryError> Errors
);

public record OpeningStockListResponse(
    IReadOnlyList<OpeningStockResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);

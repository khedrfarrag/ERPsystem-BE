namespace RetailOS.Application.Products.DTOs;

public record CategorySummaryDto(
    Guid Id,
    string Name
);

public record UnitSummaryDto(
    Guid Id,
    string Name,
    string Symbol
);

public record CreateProductRequest(
    string Name,
    Guid CategoryId,
    Guid UnitId,
    decimal SellingPrice,
    string? Barcode = null,
    string? Description = null,
    decimal? PurchaseCost = null,
    decimal? MinStockLevel = null,
    string? ImageUrl = null,
    decimal? WholesalePrice = null,
    bool IsWholesaleAvailable = false,
    decimal? InitialStock = null
);

public record UpdateProductRequest(
    string Name,
    Guid CategoryId,
    Guid UnitId,
    decimal SellingPrice,
    string? Barcode = null,
    string? Description = null,
    decimal? PurchaseCost = null,
    decimal? MinStockLevel = null,
    string? ImageUrl = null,
    decimal? WholesalePrice = null,
    bool IsWholesaleAvailable = false
);

public record UpdateProductStatusRequest(
    bool IsActive
);

public record ProductResponse(
    Guid Id,
    string Name,
    string? Barcode,
    string? Description,
    decimal SellingPrice,
    decimal? PurchaseCost,
    decimal? MinStockLevel,
    string? ImageUrl,
    bool IsActive,
    CategorySummaryDto Category,
    UnitSummaryDto Unit,
    decimal CurrentStock,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    decimal? WholesalePrice = null,
    bool IsWholesaleAvailable = false,
    decimal? EffectiveWholesalePrice = null,
    Guid? CategoryId = null,
    Guid? UnitId = null
);

public record ProductListResponse(
    IReadOnlyList<ProductResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);

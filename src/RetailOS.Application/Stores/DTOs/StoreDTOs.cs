namespace RetailOS.Application.Stores.DTOs;

public record UpdateStoreRequest(
    string Name,
    string? Phone,
    string? Address,
    bool TaxEnabled,
    bool AllowNegativeStock,
    string? InvoicePrefix,
    string Currency = "EGP",
    string Timezone = "Africa/Cairo",
    bool EnableInvoiceArchiving = true
);

public record StoreResponse(
    Guid Id,
    string Name,
    string BusinessType,
    string? Phone,
    string? Address,
    string Currency,
    string Timezone,
    bool TaxEnabled,
    bool AllowNegativeStock,
    string? InvoicePrefix,
    bool IsActive,
    DateTime CreatedAt,
    bool EnableInvoiceArchiving = true
);

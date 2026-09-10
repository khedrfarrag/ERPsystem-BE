namespace RetailOS.Application.Purchases.DTOs;

public record PurchaseLineItemRequest(
    Guid ProductId,
    decimal Quantity,
    decimal UnitCost,
    decimal Discount = 0m);

public record CreatePurchaseRequest(
    Guid SupplierId,
    string? InvoiceNumber,
    DateTimeOffset PurchaseDate,
    string? Notes,
    List<PurchaseLineItemRequest> Items);

public record UpdatePurchaseRequest(
    string? InvoiceNumber,
    DateTimeOffset PurchaseDate,
    string? Notes,
    List<PurchaseLineItemRequest> Items);

public record PurchaseLineItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitCost,
    decimal Discount,
    decimal SubTotal);

public record PurchaseResponse(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string PurchaseNumber,
    string? InvoiceNumber,
    DateTimeOffset PurchaseDate,
    string Status,
    decimal TotalAmount,
    string? Notes,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PurchaseLineItemResponse> Items);

public record PurchaseListResponse(
    IReadOnlyList<PurchaseResponse> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

public record PurchaseReturnItemRequest(
    Guid ProductId,
    decimal Quantity);

public record CreatePurchaseReturnRequest(
    string Reason,
    List<PurchaseReturnItemRequest> Items);

public record PurchaseReturnItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitCost,
    decimal SubTotal);

public record PurchaseReturnResponse(
    Guid Id,
    Guid PurchaseId,
    string ReturnNumber,
    DateTimeOffset ReturnDate,
    decimal TotalAmount,
    string Reason,
    IReadOnlyList<PurchaseReturnItemResponse> Items);

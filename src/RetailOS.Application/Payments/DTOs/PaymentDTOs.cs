namespace RetailOS.Application.Payments.DTOs;

public record CreatePaymentRequest(
    string PartyType,
    Guid? CustomerId,
    Guid? SupplierId,
    decimal Amount,
    string PaymentMethod,
    string? ReferenceNumber = null,
    string? Notes = null);

public record PaymentResponse(
    Guid Id,
    string PartyType,
    Guid? CustomerId,
    string? CustomerName,
    Guid? SupplierId,
    string? SupplierName,
    decimal Amount,
    DateTimeOffset PaymentDate,
    string PaymentMethod,
    string? ReferenceNumber,
    string? Notes,
    DateTimeOffset CreatedAt);

public record PaymentListResponse(
    IReadOnlyList<PaymentResponse> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

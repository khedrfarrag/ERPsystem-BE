using RetailOS.Application.Suppliers.DTOs;

namespace RetailOS.Application.Customers.DTOs;

public record CreateCustomerRequest(
    string Name,
    string Phone,
    string? Address = null,
    decimal? CreditLimit = null,
    string? Notes = null,
    decimal? OpeningBalance = null);

public record UpdateCustomerRequest(
    string Name,
    string Phone,
    string? Address = null,
    decimal? CreditLimit = null,
    string? Notes = null);

public record CustomerResponse(
    Guid Id,
    string Name,
    string Phone,
    string? Address,
    decimal? CreditLimit,
    string? Notes,
    decimal CurrentBalance,
    bool IsActive);

public record CustomerListResponse(
    IReadOnlyList<CustomerResponse> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

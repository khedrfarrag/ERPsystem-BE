namespace RetailOS.Application.Suppliers.DTOs;

public record CreateSupplierRequest(
    string Name,
    string Phone,
    string? Address = null,
    string? Notes = null,
    decimal? OpeningBalance = null);

public record UpdateSupplierRequest(
    string Name,
    string Phone,
    string? Address = null,
    string? Notes = null);

public record CreateRepresentativeRequest(
    string Name,
    string Phone,
    string? Notes = null);

public record RepresentativeResponse(
    Guid Id,
    Guid SupplierId,
    string Name,
    string Phone,
    string? Notes,
    bool IsActive);

public record SupplierResponse(
    Guid Id,
    string Name,
    string Phone,
    string? Address,
    string? Notes,
    decimal CurrentBalance,
    bool IsActive,
    List<RepresentativeResponse> Representatives);

public record SupplierListResponse(
    IReadOnlyList<SupplierResponse> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

public record AccountStatementItemResponse(
    Guid Id,
    DateTimeOffset Date,
    string Type,
    decimal Amount,
    decimal RunningBalance,
    Guid? ReferenceId,
    string? Notes);

public record AccountStatementResponse(
    Guid PartyId,
    string PartyName,
    decimal CurrentBalance,
    IReadOnlyList<AccountStatementItemResponse> Transactions);

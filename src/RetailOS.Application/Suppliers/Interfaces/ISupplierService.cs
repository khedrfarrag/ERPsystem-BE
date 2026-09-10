using RetailOS.Application.Suppliers.DTOs;

namespace RetailOS.Application.Suppliers.Interfaces;

public interface ISupplierService
{
    Task<SupplierResponse> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupplierListResponse> GetAllAsync(string? search = null, bool? isActive = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<SupplierResponse?> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RepresentativeResponse> AddRepresentativeAsync(Guid supplierId, CreateRepresentativeRequest request, CancellationToken cancellationToken = default);
    Task<AccountStatementResponse?> GetStatementAsync(Guid supplierId, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);
}

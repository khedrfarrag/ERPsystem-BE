using RetailOS.Application.Customers.DTOs;
using RetailOS.Application.Suppliers.DTOs;

namespace RetailOS.Application.Customers.Interfaces;

public interface ICustomerService
{
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerListResponse> GetAllAsync(string? search = null, bool? isActive = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<CustomerResponse?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccountStatementResponse?> GetStatementAsync(Guid customerId, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);
}

using RetailOS.Application.Sales.DTOs;

namespace RetailOS.Application.Sales.Interfaces;

public interface ISaleService
{
    Task<SaleResponse> CreateAsync(CreateSaleRequest request, CancellationToken cancellationToken = default);
    Task<SaleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SaleListResponse> GetAllAsync(Guid? customerId = null, string? paymentMethod = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<SaleReturnResponse> CreateReturnAsync(Guid saleId, CreateSaleReturnRequest request, CancellationToken cancellationToken = default);
}

using RetailOS.Application.B2B.DTOs;

namespace RetailOS.Application.B2B;

public interface IB2BOrderService
{
    Task<IReadOnlyList<B2BCatalogProductDto>> GetCatalogAsync(string? search = null, Guid? categoryId = null, CancellationToken cancellationToken = default);
    Task<B2BOrderListResponse> GetOrdersAsync(int page = 1, int pageSize = 20, string? status = null, Guid? merchantId = null, CancellationToken cancellationToken = default);
    Task<B2BOrderDto> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<B2BOrderDto> CreateOrderAsync(CreateB2BOrderRequest request, CancellationToken cancellationToken = default);
    Task<B2BOrderDto> ApproveOrderAsync(Guid id, ApproveB2BOrderRequest request, CancellationToken cancellationToken = default);
    Task<B2BOrderDto> InvoiceOrderAsync(Guid id, InvoiceB2BOrderRequest request, CancellationToken cancellationToken = default);
    Task<B2BOrderDto> RejectOrderAsync(Guid id, RejectB2BOrderRequest request, CancellationToken cancellationToken = default);
    Task<B2BOrderDto> CancelOrderAsync(Guid id, CancelB2BOrderRequest request, CancellationToken cancellationToken = default);
}

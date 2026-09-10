using RetailOS.Application.Purchases.DTOs;

namespace RetailOS.Application.Purchases.Interfaces;

public interface IPurchaseService
{
    Task<PurchaseResponse> CreateDraftAsync(CreatePurchaseRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseListResponse> GetAllAsync(Guid? supplierId = null, string? status = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PurchaseResponse?> UpdateDraftAsync(Guid id, UpdatePurchaseRequest request, CancellationToken cancellationToken = default);
    Task<PurchaseResponse> ConfirmAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseReturnResponse> CreateReturnAsync(Guid purchaseId, CreatePurchaseReturnRequest request, CancellationToken cancellationToken = default);
}

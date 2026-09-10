using RetailOS.Application.Stores.DTOs;

namespace RetailOS.Application.Stores.Interfaces;

public interface IStoreService
{
    Task<StoreResponse> GetCurrentStoreAsync(CancellationToken cancellationToken = default);
    Task<StoreResponse> UpdateCurrentStoreAsync(UpdateStoreRequest request, CancellationToken cancellationToken = default);
}

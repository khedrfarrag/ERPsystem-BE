using RetailOS.Application.Inventory.DTOs;

namespace RetailOS.Application.Inventory.Interfaces;

public interface IInventoryService
{
    Task<OpeningStockResponse> RecordOpeningStockAsync(RecordOpeningStockRequest request, CancellationToken cancellationToken = default);
    Task<BulkOpeningStockResponse> BulkRecordOpeningStockAsync(BulkOpeningStockRequest request, CancellationToken cancellationToken = default);
    Task<OpeningStockListResponse> GetOpeningStockEntriesAsync(int page = 1, int pageSize = 25, CancellationToken cancellationToken = default);
}

using RetailOS.Application.Dashboard.DTOs;

namespace RetailOS.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<SalesTrendDto> GetSalesTrendAsync(int days = 7, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int days = 30, int limit = 5, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SlowMovingProductDto>> GetSlowMovingProductsAsync(int days = 30, int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LowStockAlertDto>> GetLowStockAlertsAsync(int limit = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentActivityDto>> GetRecentActivityAsync(int limit = 10, CancellationToken cancellationToken = default);
}

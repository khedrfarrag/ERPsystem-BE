using RetailOS.Application.Reports.DTOs;

namespace RetailOS.Application.Reports.Interfaces;

public interface IReportService
{
    Task<SalesSummaryReportResponse> GetSalesSummaryAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        Guid? customerId = null,
        string? paymentMethod = null,
        string? basis = "Accrual",
        CancellationToken cancellationToken = default);

    Task<ProfitLossReportResponse> GetProfitLossAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? basis = "Accrual",
        CancellationToken cancellationToken = default);

    Task<InventoryValuationReportResponse> GetInventoryValuationAsync(
        DateTimeOffset? asOfDate = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    Task<ProductStockMovementReportResponse> GetProductStockMovementAsync(
        Guid productId,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LowStockAlertItemResponse>> GetLowStockAlertsAsync(
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    Task<CustomerBalancesReportResponse> GetCustomerBalancesAsync(
        bool hasBalanceOnly = true,
        CancellationToken cancellationToken = default);

    Task<SupplierBalancesReportResponse> GetSupplierBalancesAsync(
        bool hasBalanceOnly = true,
        CancellationToken cancellationToken = default);

    Task<CashRegisterAuditReportResponse> GetCashRegisterAuditAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default);
}

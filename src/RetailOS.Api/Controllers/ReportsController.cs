using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.Reports.Common;
using RetailOS.Application.Reports.DTOs;
using RetailOS.Application.Reports.Interfaces;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("sales")]
    [ProducesResponseType(typeof(ApiResponse<SalesSummaryReportResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesSummary(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? paymentMethod = null,
        [FromQuery] string? basis = "Accrual",
        [FromQuery] string? format = "json",
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetSalesSummaryAsync(from, to, customerId, paymentMethod, basis, cancellationToken);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvBytes = CsvExporter.ExportSalesSummary(result);
            return File(csvBytes, "text/csv", $"sales-summary-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        return Ok(ApiResponse<SalesSummaryReportResponse>.Ok(result));
    }

    [HttpGet("profit-loss")]
    [ProducesResponseType(typeof(ApiResponse<ProfitLossReportResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfitLoss(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string? basis = "Accrual",
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetProfitLossAsync(from, to, basis, cancellationToken);
        return Ok(ApiResponse<ProfitLossReportResponse>.Ok(result));
    }

    [HttpGet("inventory/valuation")]
    [ProducesResponseType(typeof(ApiResponse<InventoryValuationReportResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryValuation(
        [FromQuery] DateTimeOffset? asOfDate = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? format = "json",
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetInventoryValuationAsync(asOfDate, categoryId, cancellationToken);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvBytes = CsvExporter.ExportInventoryValuation(result);
            return File(csvBytes, "text/csv", $"inventory-valuation-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        return Ok(ApiResponse<InventoryValuationReportResponse>.Ok(result));
    }

    [HttpGet("inventory/movement/{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductStockMovementReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStockMovement(
        [FromRoute] Guid productId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string? format = "json",
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetProductStockMovementAsync(productId, from, to, cancellationToken);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvBytes = CsvExporter.ExportStockMovement(result);
            return File(csvBytes, "text/csv", $"stock-movement-{productId}-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        return Ok(ApiResponse<ProductStockMovementReportResponse>.Ok(result));
    }

    [HttpGet("inventory/low-stock")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LowStockAlertItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStockAlerts(
        [FromQuery] Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetLowStockAlertsAsync(categoryId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LowStockAlertItemResponse>>.Ok(result));
    }

    [HttpGet("balances/customers")]
    [ProducesResponseType(typeof(ApiResponse<CustomerBalancesReportResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerBalances(
        [FromQuery] bool hasBalanceOnly = true,
        [FromQuery] string? format = "json",
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetCustomerBalancesAsync(hasBalanceOnly, cancellationToken);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvBytes = CsvExporter.ExportCustomerBalances(result);
            return File(csvBytes, "text/csv", $"customer-balances-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        return Ok(ApiResponse<CustomerBalancesReportResponse>.Ok(result));
    }

    [HttpGet("balances/suppliers")]
    [ProducesResponseType(typeof(ApiResponse<SupplierBalancesReportResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSupplierBalances(
        [FromQuery] bool hasBalanceOnly = true,
        [FromQuery] string? format = "json",
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetSupplierBalancesAsync(hasBalanceOnly, cancellationToken);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvBytes = CsvExporter.ExportSupplierBalances(result);
            return File(csvBytes, "text/csv", $"supplier-balances-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        return Ok(ApiResponse<SupplierBalancesReportResponse>.Ok(result));
    }

    [HttpGet("cash-register")]
    [ProducesResponseType(typeof(ApiResponse<CashRegisterAuditReportResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCashRegisterAudit(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string? format = "json",
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetCashRegisterAuditAsync(from, to, cancellationToken);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvBytes = CsvExporter.ExportCashRegisterAudit(result);
            return File(csvBytes, "text/csv", $"cash-register-audit-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        return Ok(ApiResponse<CashRegisterAuditReportResponse>.Ok(result));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.Dashboard;
using RetailOS.Application.Dashboard.DTOs;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Owner},{Roles.Manager},{Roles.Cashier}")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<DashboardSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(result, "Dashboard summary retrieved successfully"));
    }

    [HttpGet("sales-trend")]
    [ProducesResponseType(typeof(ApiResponse<SalesTrendDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSalesTrend(
        [FromQuery] int days = 7,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetSalesTrendAsync(days, startDate, endDate, cancellationToken);
        return Ok(ApiResponse<SalesTrendDto>.Ok(result, "Sales trend retrieved successfully"));
    }

    [HttpGet("top-products")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TopProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTopProducts(
        [FromQuery] int days = 30,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetTopProductsAsync(days, limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TopProductDto>>.Ok(result, "Top products retrieved successfully"));
    }

    [HttpGet("slow-moving-products")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SlowMovingProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSlowMovingProducts(
        [FromQuery] int days = 30,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetSlowMovingProductsAsync(days, limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SlowMovingProductDto>>.Ok(result, "Slow-moving products retrieved successfully"));
    }

    [HttpGet("low-stock-alerts")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LowStockAlertDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLowStockAlerts(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetLowStockAlertsAsync(limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LowStockAlertDto>>.Ok(result, "Low-stock alerts retrieved successfully"));
    }

    [HttpGet("recent-activity")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RecentActivityDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRecentActivity(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetRecentActivityAsync(limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RecentActivityDto>>.Ok(result, "Recent activity retrieved successfully"));
    }
}

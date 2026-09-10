using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.Inventory.DTOs;
using RetailOS.Application.Inventory.Interfaces;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPost("opening-stock")]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(typeof(ApiResponse<OpeningStockResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordOpeningStock(
        [FromBody] RecordOpeningStockRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _inventoryService.RecordOpeningStockAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<OpeningStockResponse>.Ok(response, "Opening stock recorded successfully."));
    }

    [HttpPost("opening-stock/bulk")]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(typeof(ApiResponse<BulkOpeningStockResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkRecordOpeningStock(
        [FromBody] BulkOpeningStockRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Entries == null || request.Entries.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "At least one entry is required."));

        if (request.Entries.Count > 500)
            return BadRequest(ApiResponse<object>.Fail("MAX_ENTRIES_EXCEEDED", "Maximum 500 entries allowed per request."));

        var response = await _inventoryService.BulkRecordOpeningStockAsync(request, cancellationToken);
        return Ok(ApiResponse<BulkOpeningStockResponse>.Ok(response));
    }

    [HttpGet("opening-stock")]
    [ProducesResponseType(typeof(ApiResponse<OpeningStockListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpeningStockEntries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var response = await _inventoryService.GetOpeningStockEntriesAsync(page, pageSize, cancellationToken);
        return Ok(ApiResponse<OpeningStockListResponse>.Ok(response));
    }
}

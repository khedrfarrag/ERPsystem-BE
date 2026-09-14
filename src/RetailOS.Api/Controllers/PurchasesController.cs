using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Purchases.DTOs;
using RetailOS.Application.Purchases.Interfaces;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;
    private readonly IIdempotencyService _idempotencyService;

    public PurchasesController(IPurchaseService purchaseService, IIdempotencyService idempotencyService)
    {
        _purchaseService = purchaseService;
        _idempotencyService = idempotencyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PurchaseListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? supplierId = null,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _purchaseService.GetAllAsync(supplierId, status, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PurchaseListResponse>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _purchaseService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("PURCHASE_NOT_FOUND", "Purchase not found."));

        return Ok(ApiResponse<PurchaseResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDraft([FromBody] CreatePurchaseRequest request, CancellationToken cancellationToken)
    {
        string? key = null;
        if (Request.Headers.TryGetValue("Idempotency-Key", out var headerVal) && !string.IsNullOrWhiteSpace(headerVal))
        {
            key = headerVal.ToString().Trim();
            var cached = await _idempotencyService.GetResultAsync(key, Request.Path, cancellationToken);
            if (cached is not null)
                return new ContentResult
                {
                    Content = cached.ResponseBody,
                    ContentType = "application/json",
                    StatusCode = cached.StatusCode
                };

            var acquired = await _idempotencyService.TryAcquireAsync(key, Request.Path, cancellationToken);
            if (!acquired)
                return Conflict(ApiResponse<object>.Fail("IDEMPOTENCY_CONFLICT", "A request with this Idempotency-Key is already in progress or completed."));
        }

        var result = await _purchaseService.CreateDraftAsync(request, cancellationToken);
        var response = ApiResponse<PurchaseResponse>.Ok(result, "Purchase order draft created.");

        if (key is not null)
        {
            await _idempotencyService.CompleteAsync(key, StatusCodes.Status201Created, response, cancellationToken);
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDraft([FromRoute] Guid id, [FromBody] UpdatePurchaseRequest request, CancellationToken cancellationToken)
    {
        var result = await _purchaseService.UpdateDraftAsync(id, request, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("PURCHASE_NOT_FOUND", "Purchase not found."));

        return Ok(ApiResponse<PurchaseResponse>.Ok(result, "Purchase draft updated successfully."));
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _purchaseService.ConfirmAsync(id, cancellationToken);
        return Ok(ApiResponse<PurchaseResponse>.Ok(result, "Purchase order confirmed and inventory updated."));
    }

    [HttpPost("{id:guid}/returns")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseReturnResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReturn([FromRoute] Guid id, [FromBody] CreatePurchaseReturnRequest request, CancellationToken cancellationToken)
    {
        var result = await _purchaseService.CreateReturnAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<PurchaseReturnResponse>.Ok(result, "Purchase return processed successfully."));
    }
}

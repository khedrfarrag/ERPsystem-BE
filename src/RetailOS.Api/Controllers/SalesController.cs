using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Sales.DTOs;
using RetailOS.Application.Sales.Interfaces;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly IIdempotencyService _idempotencyService;

    public SalesController(ISaleService saleService, IIdempotencyService idempotencyService)
    {
        _saleService = saleService;
        _idempotencyService = idempotencyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<SaleListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? paymentMethod = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _saleService.GetAllAsync(customerId, paymentMethod, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<SaleListResponse>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _saleService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("SALE_NOT_FOUND", "Sale not found."));

        return Ok(ApiResponse<SaleResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager},{Roles.Cashier}")]
    [ProducesResponseType(typeof(ApiResponse<SaleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
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

        var result = await _saleService.CreateAsync(request, cancellationToken);
        var response = ApiResponse<SaleResponse>.Ok(result, "Sale completed successfully.");

        if (key is not null)
        {
            await _idempotencyService.CompleteAsync(key, StatusCodes.Status201Created, response, cancellationToken);
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("{id:guid}/returns")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<SaleReturnResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReturn([FromRoute] Guid id, [FromBody] CreateSaleReturnRequest request, CancellationToken cancellationToken)
    {
        var result = await _saleService.CreateReturnAsync(id, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<SaleReturnResponse>.Ok(result, "Sale return processed successfully."));
    }
}

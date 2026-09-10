using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Payments.DTOs;
using RetailOS.Application.Payments.Interfaces;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IIdempotencyService _idempotencyService;

    public PaymentsController(IPaymentService paymentService, IIdempotencyService idempotencyService)
    {
        _paymentService = paymentService;
        _idempotencyService = idempotencyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaymentListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? partyType = null,
        [FromQuery] Guid? partyId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _paymentService.GetAllAsync(partyType, partyId, pageNumber, pageSize, cancellationToken);
        return Ok(ApiResponse<PaymentListResponse>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(ApiResponse<object>.Fail("PAYMENT_NOT_FOUND", "Payment not found."));

        return Ok(ApiResponse<PaymentResponse>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<PaymentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
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

        var result = await _paymentService.CreateAsync(request, cancellationToken);
        var response = ApiResponse<PaymentResponse>.Ok(result, "Payment recorded successfully.");

        if (key is not null)
        {
            await _idempotencyService.CompleteAsync(key, StatusCodes.Status201Created, response, cancellationToken);
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }
}

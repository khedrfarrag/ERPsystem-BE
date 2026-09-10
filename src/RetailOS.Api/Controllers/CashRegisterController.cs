using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.CashRegister.DTOs;
using RetailOS.Application.CashRegister.Interfaces;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/cash-register")]
[Authorize]
public class CashRegisterController : ControllerBase
{
    private readonly ICashRegisterService _cashRegisterService;

    public CashRegisterController(ICashRegisterService cashRegisterService)
    {
        _cashRegisterService = cashRegisterService;
    }

    [HttpGet("current")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager},{Roles.Cashier}")]
    [ProducesResponseType(typeof(ApiResponse<CashRegisterSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentSummary(CancellationToken cancellationToken)
    {
        var result = await _cashRegisterService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<CashRegisterSummaryResponse>.Ok(result));
    }

    [HttpPost("open")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<CashRegisterTransactionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OpenFloat([FromBody] OpenFloatRequest request, CancellationToken cancellationToken)
    {
        var result = await _cashRegisterService.OpenFloatAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CashRegisterTransactionResponse>.Ok(result, "Opening float recorded."));
    }

    [HttpPost("close")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<CashRegisterCloseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CloseRegister([FromBody] CloseRegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _cashRegisterService.CloseRegisterAsync(request, cancellationToken);
        return Ok(ApiResponse<CashRegisterCloseResponse>.Ok(result, "Register reconciled and closed."));
    }

    [HttpGet("transactions")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CashRegisterTransactionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _cashRegisterService.GetTransactionsAsync(from, to, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CashRegisterTransactionResponse>>.Ok(result));
    }
}

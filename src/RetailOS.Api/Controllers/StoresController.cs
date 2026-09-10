using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.Stores.DTOs;
using RetailOS.Application.Stores.Interfaces;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StoresController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoresController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<StoreResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var response = await _storeService.GetCurrentStoreAsync(cancellationToken);
        return Ok(ApiResponse<StoreResponse>.Ok(response));
    }

    [HttpPut("current")]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(typeof(ApiResponse<StoreResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCurrent([FromBody] UpdateStoreRequest request, CancellationToken cancellationToken)
    {
        var response = await _storeService.UpdateCurrentStoreAsync(request, cancellationToken);
        return Ok(ApiResponse<StoreResponse>.Ok(response, "Store settings updated successfully."));
    }
}

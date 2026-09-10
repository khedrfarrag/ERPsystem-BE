using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.Units.DTOs;
using RetailOS.Application.Units.Interfaces;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UnitsController : ControllerBase
{
    private readonly IUnitService _unitService;

    public UnitsController(IUnitService unitService)
    {
        _unitService = unitService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UnitListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnits(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _unitService.GetUnitsAsync(page, pageSize, isActive, cancellationToken);
        return Ok(ApiResponse<UnitListResponse>.Ok(response));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UnitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUnitById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _unitService.GetUnitByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<UnitResponse>.Ok(response));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<UnitResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUnit([FromBody] CreateUnitRequest request, CancellationToken cancellationToken)
    {
        var response = await _unitService.CreateUnitAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<UnitResponse>.Ok(response, "Unit created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<UnitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUnit([FromRoute] Guid id, [FromBody] UpdateUnitRequest request, CancellationToken cancellationToken)
    {
        var response = await _unitService.UpdateUnitAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UnitResponse>.Ok(response, "Unit updated successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<UnitResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUnitStatus([FromRoute] Guid id, [FromBody] UpdateUnitStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await _unitService.UpdateUnitStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UnitResponse>.Ok(response, "Unit status updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUnit([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _unitService.DeleteUnitAsync(id, cancellationToken);
        return NoContent();
    }
}

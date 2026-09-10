using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.Users.DTOs;
using RetailOS.Application.Users.Interfaces;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<UserListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? role = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _userService.GetUsersAsync(page, pageSize, isActive, role, cancellationToken);
        return Ok(ApiResponse<UserListResponse>.Ok(response));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await _userService.CreateUserAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<UserResponse>.Ok(response, "User created successfully."));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _userService.GetUserByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser([FromRoute] Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await _userService.UpdateUserAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(response, "User updated successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserStatus([FromRoute] Guid id, [FromBody] UpdateUserStatusRequest request, CancellationToken cancellationToken)
    {
        var response = await _userService.UpdateUserStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(response, "User status updated successfully."));
    }
}

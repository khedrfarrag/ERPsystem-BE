using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.B2B;
using RetailOS.Application.B2B.DTOs;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Suppliers.DTOs;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MerchantsController : ControllerBase
{
    private readonly IMerchantService _merchantService;
    private readonly IUserContext _userContext;

    public MerchantsController(IMerchantService merchantService, IUserContext userContext)
    {
        _merchantService = merchantService;
        _userContext = userContext;
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<MerchantListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _merchantService.GetAllAsync(page, pageSize, search, isActive, cancellationToken);
        return Ok(ApiResponse<MerchantListResponse>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<MerchantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _merchantService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<MerchantDto>.Ok(result));
    }

    [HttpGet("me")]
    [Authorize(Roles = Roles.Merchant)]
    [ProducesResponseType(typeof(ApiResponse<MerchantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentMerchant(CancellationToken cancellationToken)
    {
        if (_userContext.CurrentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "User identity not found."));

        var result = await _merchantService.GetByUserIdAsync(_userContext.CurrentUserId.Value, cancellationToken);
        if (result == null)
            return NotFound(ApiResponse<object>.Fail("MERCHANT_PROFILE_NOT_FOUND", "لم يتم العثور على ملف تعريف التاجر المرتبط بهذا الحساب."));

        return Ok(ApiResponse<MerchantDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<MerchantDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateMerchantRequest request, CancellationToken cancellationToken)
    {
        var result = await _merchantService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<MerchantDto>.Ok(result, "تم تسجيل تاجر الجملة وإنشاء حساب البوابة بنجاح."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<MerchantDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMerchantRequest request, CancellationToken cancellationToken)
    {
        var result = await _merchantService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<MerchantDto>.Ok(result, "تم تحديث بيانات التاجر بنجاح."));
    }

    [HttpPatch("{id:guid}/toggle-active")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleActive([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _merchantService.ToggleActiveAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "تم تعديل حالة نشاط التاجر بنجاح."));
    }

    [HttpGet("me/statement")]
    [Authorize(Roles = Roles.Merchant)]
    [ProducesResponseType(typeof(ApiResponse<AccountStatementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyStatement(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        if (_userContext.CurrentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "User identity not found."));

        var result = await _merchantService.GetMyStatementAsync(_userContext.CurrentUserId.Value, from, to, cancellationToken);
        if (result == null)
            return NotFound(ApiResponse<object>.Fail("MERCHANT_NOT_FOUND", "لم يتم العثور على كشف حساب التاجر."));

        return Ok(ApiResponse<AccountStatementResponse>.Ok(result));
    }

    [HttpGet("{id:guid}/statement")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<AccountStatementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatement(
        [FromRoute] Guid id,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _merchantService.GetStatementAsync(id, from, to, cancellationToken);
        if (result == null)
            return NotFound(ApiResponse<object>.Fail("MERCHANT_NOT_FOUND", "لم يتم العثور على كشف حساب التاجر."));

        return Ok(ApiResponse<AccountStatementResponse>.Ok(result));
    }
}


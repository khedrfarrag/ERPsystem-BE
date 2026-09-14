using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.B2B;
using RetailOS.Application.B2B.DTOs;
using RetailOS.Shared;
using RetailOS.Shared.Constants;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/v1/b2b-orders")]
[Route("api/b2b-orders")]
[Authorize]
public class B2BOrdersController : ControllerBase
{
    private readonly IB2BOrderService _orderService;

    public B2BOrdersController(IB2BOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("catalog")]
    [Authorize(Roles = $"{Roles.Merchant},{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<B2BCatalogProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalog(
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetCatalogAsync(search, categoryId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<B2BCatalogProductDto>>.Ok(result));
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Merchant},{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<B2BOrderListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] Guid? merchantId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetOrdersAsync(page, pageSize, status, merchantId, cancellationToken);
        return Ok(ApiResponse<B2BOrderListResponse>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Merchant},{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<B2BOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetOrderByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<B2BOrderDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Merchant)]
    [ProducesResponseType(typeof(ApiResponse<B2BOrderDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateB2BOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.CreateOrderAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<B2BOrderDto>.Ok(result, "تم إرسال طلب التوريد بنجاح وجاري مراجعته من الإدارة."));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<B2BOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveOrder([FromRoute] Guid id, [FromBody] ApproveB2BOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.ApproveOrderAsync(id, request, cancellationToken);
        return Ok(ApiResponse<B2BOrderDto>.Ok(result, "تم اعتماد طلب التوريد وتثبيت الكميات المعتمدة بنجاح."));
    }

    [HttpPost("{id:guid}/invoice")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<B2BOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> InvoiceOrder([FromRoute] Guid id, [FromBody] InvoiceB2BOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.InvoiceOrderAsync(id, request, cancellationToken);
        return Ok(ApiResponse<B2BOrderDto>.Ok(result, "تم تحويل الطلب إلى فاتورة مبيعات وخصم المخزون وتسجيل الحسابات بنجاح."));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = $"{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<B2BOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectOrder([FromRoute] Guid id, [FromBody] RejectB2BOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.RejectOrderAsync(id, request, cancellationToken);
        return Ok(ApiResponse<B2BOrderDto>.Ok(result, "تم رفض طلب التوريد وتوثيق السبب."));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = $"{Roles.Merchant},{Roles.Owner},{Roles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<B2BOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelOrder([FromRoute] Guid id, [FromBody] CancelB2BOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderService.CancelOrderAsync(id, request, cancellationToken);
        return Ok(ApiResponse<B2BOrderDto>.Ok(result, "تم إلغاء الطلب بنجاح."));
    }
}

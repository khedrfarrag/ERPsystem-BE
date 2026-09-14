using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailOS.Application.B2B;
using RetailOS.Application.B2B.DTOs;
using RetailOS.Shared;

namespace RetailOS.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.GetNotificationsAsync(limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(result));
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadNotificationCountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await _notificationService.GetUnreadCountAsync(cancellationToken);
        return Ok(ApiResponse<UnreadNotificationCountDto>.Ok(new UnreadNotificationCountDto { UnreadCount = count }));
    }

    [HttpPost("{id:guid}/mark-read")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _notificationService.MarkAsReadAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { id }, "تم تعيين الإشعار كمقروء."));
    }

    [HttpPost("mark-all-read")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { success = true }, "تم تعيين كافة الإشعارات كمقروءة."));
    }
}

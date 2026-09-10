using RetailOS.Application.B2B.DTOs;

namespace RetailOS.Application.B2B;

public interface INotificationService
{
    Task SendNotificationToStoreStaffAsync(string title, string message, string notificationType, string? referenceId = null, object? payload = null, CancellationToken cancellationToken = default);
    Task SendNotificationToMerchantAsync(Guid merchantUserId, string title, string message, string notificationType, string? referenceId = null, object? payload = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken = default);
}

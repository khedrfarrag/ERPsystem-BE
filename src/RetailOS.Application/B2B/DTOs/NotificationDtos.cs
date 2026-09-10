namespace RetailOS.Application.B2B.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public string? PayloadJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class UnreadNotificationCountDto
{
    public int UnreadCount { get; set; }
}

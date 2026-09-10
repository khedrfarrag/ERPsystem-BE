using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class Notification : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid? RecipientUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public bool IsRead { get; set; } = false;
    public string? PayloadJson { get; set; }
}

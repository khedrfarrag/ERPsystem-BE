using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class RefreshToken : BaseEntity, ITenantEntity
{
    public Guid UserId { get; set; }
    public Guid StoreId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? ReplacedByTokenHash { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Store? Store { get; set; }
}

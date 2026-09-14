using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class Store : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Currency { get; set; } = "EGP";
    public string Timezone { get; set; } = "Africa/Cairo";
    public bool TaxEnabled { get; set; } = false;
    public bool AllowNegativeStock { get; set; } = false;
    public string? InvoicePrefix { get; set; } = "INV";
    public bool EnableInvoiceArchiving { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

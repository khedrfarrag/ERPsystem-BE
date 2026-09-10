using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class Merchant : SoftDeletableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid UserId { get; set; }
    public string TradeName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal CreditLimit { get; set; }
    public string? PaymentTerms { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public ICollection<B2BOrder> Orders { get; set; } = new List<B2BOrder>();
}

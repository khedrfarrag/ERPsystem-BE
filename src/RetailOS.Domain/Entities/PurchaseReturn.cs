using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class PurchaseReturn : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid PurchaseId { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTimeOffset ReturnDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public Purchase Purchase { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<PurchaseReturnLineItem> LineItems { get; set; } = new List<PurchaseReturnLineItem>();
}

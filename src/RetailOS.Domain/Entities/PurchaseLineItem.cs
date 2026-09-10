using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class PurchaseLineItem : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid PurchaseId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Discount { get; set; }
    public decimal SubTotal { get; set; }

    // Navigation properties
    public Purchase Purchase { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

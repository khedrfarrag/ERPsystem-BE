using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class SaleReturnLineItem : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid SaleReturnId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; } // original unit cost at time of sale
    public decimal SubTotal { get; set; }
    public decimal TotalCost { get; set; }

    // Navigation properties
    public SaleReturn SaleReturn { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

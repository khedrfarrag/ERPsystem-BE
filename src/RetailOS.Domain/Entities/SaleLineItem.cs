using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class SaleLineItem : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; } // WAC at time of sale
    public decimal Discount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalCost { get; set; }

    // Navigation properties
    public Sale Sale { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

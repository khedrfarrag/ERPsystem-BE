using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class B2BOrderItem : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid B2BOrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    // Quantities and prices
    public decimal RequestedQuantity { get; set; }
    public decimal? ApprovedQuantity { get; set; }
    public decimal UnitWholesalePrice { get; set; }
    public decimal RequestedSubtotal { get; set; }
    public decimal? ApprovedSubtotal { get; set; }

    // Adjustment audit
    public string? AdjustmentReason { get; set; }
    public Guid? AdjustedByUserId { get; set; }
    public DateTimeOffset? AdjustedAt { get; set; }

    // Navigation properties
    public B2BOrder B2BOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

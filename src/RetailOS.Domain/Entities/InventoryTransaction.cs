using RetailOS.Domain.Common;
using RetailOS.Domain.Enums;

namespace RetailOS.Domain.Entities;

public class InventoryTransaction : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal CostPerUnit { get; set; }
    public InventoryTransactionReason Reason { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
}

using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class Product : SoftDeletableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal? PurchaseCost { get; set; }
    public decimal? MinStockLevel { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? WholesalePrice { get; set; }
    public bool IsWholesaleAvailable { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Category Category { get; set; } = null!;
    public Unit Unit { get; set; } = null!;
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}

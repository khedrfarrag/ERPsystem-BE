using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class Category : SoftDeletableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

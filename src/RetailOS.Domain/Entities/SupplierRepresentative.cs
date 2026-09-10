using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class SupplierRepresentative : SoftDeletableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property
    public Supplier Supplier { get; set; } = null!;
}

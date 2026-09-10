using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class Supplier : SoftDeletableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<SupplierRepresentative> Representatives { get; set; } = new List<SupplierRepresentative>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<SupplierAccountTransaction> AccountTransactions { get; set; } = new List<SupplierAccountTransaction>();
}

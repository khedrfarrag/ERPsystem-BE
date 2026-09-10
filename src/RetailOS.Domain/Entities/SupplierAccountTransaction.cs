using RetailOS.Domain.Common;
using RetailOS.Domain.Enums;

namespace RetailOS.Domain.Entities;

public class SupplierAccountTransaction : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid SupplierId { get; set; }
    public SupplierTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public Supplier Supplier { get; set; } = null!;
    public User User { get; set; } = null!;
}

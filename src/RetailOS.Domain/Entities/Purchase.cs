using RetailOS.Domain.Common;
using RetailOS.Domain.Enums;

namespace RetailOS.Domain.Entities;

public class Purchase : SoftDeletableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid SupplierId { get; set; }
    public string PurchaseNumber { get; set; } = string.Empty;
    public string? InvoiceNumber { get; set; }
    public DateTimeOffset PurchaseDate { get; set; }
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public Supplier Supplier { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<PurchaseLineItem> LineItems { get; set; } = new List<PurchaseLineItem>();
    public ICollection<PurchaseReturn> Returns { get; set; } = new List<PurchaseReturn>();
}

using RetailOS.Domain.Common;
using RetailOS.Domain.Enums;

namespace RetailOS.Domain.Entities;

public class Payment : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public PaymentPartyType PartyType { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SupplierId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset PaymentDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public Customer? Customer { get; set; }
    public Supplier? Supplier { get; set; }
    public User User { get; set; } = null!;
}

using RetailOS.Domain.Common;
using RetailOS.Domain.Enums;

namespace RetailOS.Domain.Entities;

public class CustomerAccountTransaction : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid CustomerId { get; set; }
    public CustomerTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public User User { get; set; } = null!;
}

using RetailOS.Domain.Common;
using RetailOS.Domain.Enums;

namespace RetailOS.Domain.Entities;

public class CashRegisterTransaction : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public CashTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}

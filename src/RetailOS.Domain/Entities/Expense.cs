using RetailOS.Domain.Common;
using RetailOS.Domain.Enums;

namespace RetailOS.Domain.Entities;

public class Expense : SoftDeletableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset ExpenseDate { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? Description { get; set; }
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public ExpenseCategory Category { get; set; } = null!;
    public User User { get; set; } = null!;
}

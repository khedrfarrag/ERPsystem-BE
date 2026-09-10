using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class Customer : SoftDeletableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal? CreditLimit { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<CustomerAccountTransaction> AccountTransactions { get; set; } = new List<CustomerAccountTransaction>();
}

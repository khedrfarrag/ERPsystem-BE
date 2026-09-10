using RetailOS.Domain.Common;
using RetailOS.Domain.Enums;

namespace RetailOS.Domain.Entities;

public class SaleReturn : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid SaleId { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTimeOffset ReturnDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalCost { get; set; }
    public RefundMethod RefundMethod { get; set; } = RefundMethod.Cash;
    public string Reason { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public Sale Sale { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<SaleReturnLineItem> LineItems { get; set; } = new List<SaleReturnLineItem>();
}

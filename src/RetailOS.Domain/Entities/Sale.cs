using RetailOS.Domain.Common;
using RetailOS.Domain.Enums;

namespace RetailOS.Domain.Entities;

public class Sale : SoftDeletableEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid? CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTimeOffset SaleDate { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Completed;
    public SalePaymentMethod PaymentMethod { get; set; } = SalePaymentMethod.Cash;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CashAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal TotalCost { get; set; }
    public string? EtaUuid { get; set; }
    public string? SubmissionStatus { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    // Navigation properties
    public Customer? Customer { get; set; }
    public User User { get; set; } = null!;
    public ICollection<SaleLineItem> LineItems { get; set; } = new List<SaleLineItem>();
    public ICollection<SaleReturn> Returns { get; set; } = new List<SaleReturn>();
}

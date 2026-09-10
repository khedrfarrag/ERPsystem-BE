using RetailOS.Domain.Common;

namespace RetailOS.Domain.Entities;

public class B2BOrder : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public Guid MerchantId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Invoiced, Rejected, Cancelled
    public string PaymentPreference { get; set; } = "Credit"; // Cash, Credit, Partial
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }

    // Cancellation audit fields
    public DateTimeOffset? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }

    // Credit limit override audit fields
    public bool IsCreditLimitOverrideUsed { get; set; } = false;
    public string? CreditLimitOverrideReason { get; set; }
    public Guid? CreditLimitOverrideByUserId { get; set; }
    public DateTimeOffset? CreditLimitOverrideAt { get; set; }
    public decimal? CreditLimitAtInvoice { get; set; }
    public decimal? OutstandingBalanceAtInvoice { get; set; }
    public decimal? CreditAmountAtInvoice { get; set; }
    public decimal? ProjectedBalanceAtInvoice { get; set; }
    public decimal? CreditLimitExceededBy { get; set; }

    // Converted Sales Invoice link
    public Guid? SalesInvoiceId { get; set; }

    // Navigation properties
    public Merchant Merchant { get; set; } = null!;
    public Sale? SalesInvoice { get; set; }
    public ICollection<B2BOrderItem> Items { get; set; } = new List<B2BOrderItem>();
}

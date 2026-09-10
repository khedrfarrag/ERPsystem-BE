namespace RetailOS.Application.B2B.DTOs;

public class B2BCatalogProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string? UnitName { get; set; }
    public string? CategoryName { get; set; }
    public decimal AvailableStock { get; set; }
    public decimal EffectiveWholesalePrice { get; set; }
}

public class B2BOrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public decimal? ApprovedQuantity { get; set; }
    public decimal UnitWholesalePrice { get; set; }
    public decimal RequestedSubtotal { get; set; }
    public decimal? ApprovedSubtotal { get; set; }
    public string? AdjustmentReason { get; set; }
    public DateTimeOffset? AdjustedAt { get; set; }
    public decimal AvailableStock { get; set; }
    public decimal? PurchaseCost { get; set; }
}

public class B2BOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid MerchantId { get; set; }
    public string MerchantTradeName { get; set; } = string.Empty;
    public string? MerchantPhone { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Approved, Invoiced, Rejected, Cancelled
    public string PaymentPreference { get; set; } = string.Empty; // Cash, Credit, Partial
    public decimal? ExpectedDownPayment { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string? Notes { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public bool IsCreditLimitOverrideUsed { get; set; }
    public string? CreditLimitOverrideReason { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<B2BOrderItemDto> Items { get; set; } = new();
}

public record B2BOrderListResponse(
    IReadOnlyList<B2BOrderDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public class CreateB2BOrderRequest
{
    public string PaymentPreference { get; set; } = "Credit";
    public decimal? ExpectedDownPayment { get; set; }
    public string? Notes { get; set; }
    public List<CreateB2BOrderItemRequest> Items { get; set; } = new();
}

public class CreateB2BOrderItemRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class ApproveB2BOrderRequest
{
    public List<ApproveB2BOrderItemRequest> Items { get; set; } = new();
}

public class ApproveB2BOrderItemRequest
{
    public Guid OrderItemId { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal? UnitWholesalePrice { get; set; }
    public string? AdjustmentReason { get; set; }
}

public class InvoiceB2BOrderRequest
{
    public decimal PaidAmount { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public string PaymentMethod { get; set; } = "Cash";
    public bool CreditLimitOverrideConfirmed { get; set; }
    public string? CreditLimitOverrideReason { get; set; }
    public string? Notes { get; set; }
}

public class CancelB2BOrderRequest
{
    public string? Reason { get; set; }
}

public class RejectB2BOrderRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}

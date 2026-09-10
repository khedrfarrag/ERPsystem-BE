namespace RetailOS.Application.B2B.DTOs;

public class MerchantDto
{
    public Guid Id { get; set; }
    public string TradeName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public string? PaymentTerms { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public record MerchantListResponse(
    IReadOnlyList<MerchantDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public class CreateMerchantRequest
{
    public string TradeName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal CreditLimit { get; set; }
    public string? PaymentTerms { get; set; }
}

public class UpdateMerchantRequest
{
    public string TradeName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal CreditLimit { get; set; }
    public string? PaymentTerms { get; set; }
    public bool IsActive { get; set; }
}

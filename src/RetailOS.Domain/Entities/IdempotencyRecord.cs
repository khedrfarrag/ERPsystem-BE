using RetailOS.Domain.Common;
using RetailOS.Domain.Enums;

namespace RetailOS.Domain.Entities;

public class IdempotencyRecord : BaseEntity, ITenantEntity
{
    public Guid StoreId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public IdempotencyStatus Status { get; set; } = IdempotencyStatus.Pending;
    public int? ResponseCode { get; set; }
    public string? ResponseBody { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

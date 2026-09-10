namespace RetailOS.Domain.Common;

public abstract class SoftDeletableEntity : BaseEntity
{
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
}

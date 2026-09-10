namespace RetailOS.Application.Common.Interfaces;

public interface IUserContext
{
    Guid? CurrentUserId { get; }
}

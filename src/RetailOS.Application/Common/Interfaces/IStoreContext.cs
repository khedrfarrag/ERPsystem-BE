namespace RetailOS.Application.Common.Interfaces;

public interface IStoreContext
{
    Guid? CurrentStoreId { get; }
    bool HasStore { get; }
    void SetCurrentStoreId(Guid storeId);
}

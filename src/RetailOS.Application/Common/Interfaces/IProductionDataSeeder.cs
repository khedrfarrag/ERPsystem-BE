namespace RetailOS.Application.Common.Interfaces;

public interface IProductionDataSeeder
{
    Task<ProductionSeedResult> SeedProductionMasterDataAsync(CancellationToken cancellationToken = default);
}

public record ProductionSeedResult(
    bool Success,
    string Message,
    Guid StoreId,
    string OwnerEmail,
    int UnitsCreated,
    int CategoriesCreated
);

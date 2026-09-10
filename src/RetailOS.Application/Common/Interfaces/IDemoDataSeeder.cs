namespace RetailOS.Application.Common.Interfaces;

public interface IDemoDataSeeder
{
    Task<DemoSeedResult> SeedDemoDataAsync(bool force = false, CancellationToken cancellationToken = default);
}

public record DemoSeedResult(
    bool Success,
    string Message,
    Guid StoreId,
    string StoreName,
    string OwnerEmail,
    string Password,
    int ProductsCreated,
    int SalesCreated,
    int CustomersCreated,
    int SuppliersCreated
);

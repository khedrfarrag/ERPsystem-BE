namespace RetailOS.Application.Common.Interfaces;

public interface IInvoiceStorageService
{
    Task<string> SaveArchiveImageAsync(
        Stream imageStream, 
        string originalFileName, 
        Guid storeId, 
        CancellationToken cancellationToken = default);

    Task<string> SaveTemporaryImageAsync(
        Stream imageStream, 
        string originalFileName, 
        Guid storeId, 
        CancellationToken cancellationToken = default);

    Task<Stream?> GetTemporaryImageAsync(
        string tempKey, 
        Guid storeId, 
        CancellationToken cancellationToken = default);

    Task CleanupTemporaryImageAsync(
        string tempKey, 
        CancellationToken cancellationToken = default);
}

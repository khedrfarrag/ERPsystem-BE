using RetailOS.Application.Products.DTOs;

namespace RetailOS.Application.Products.Interfaces;

public interface IProductImportService
{
    Task<ImportPreviewResponse> PreviewImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<ImportCommitResponse> CommitImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}

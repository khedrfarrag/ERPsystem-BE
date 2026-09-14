using Microsoft.AspNetCore.Hosting;
using RetailOS.Application.Common.Interfaces;

namespace RetailOS.Infrastructure.Storage;

public class LocalFileInvoiceStorageService : IInvoiceStorageService
{
    private readonly string _baseUploadDir;

    public LocalFileInvoiceStorageService(IWebHostEnvironment environment)
    {
        var contentRoot = environment.ContentRootPath;
        _baseUploadDir = Path.Combine(contentRoot, "wwwroot", "uploads");
        if (!Directory.Exists(_baseUploadDir))
        {
            Directory.CreateDirectory(_baseUploadDir);
        }
    }

    public async Task<string> SaveArchiveImageAsync(
        Stream imageStream, 
        string originalFileName, 
        Guid storeId, 
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".webp";

        var year = DateTime.UtcNow.Year.ToString();
        var storeFolder = Path.Combine(_baseUploadDir, "invoices", storeId.ToString(), year);
        if (!Directory.Exists(storeFolder))
        {
            Directory.CreateDirectory(storeFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(storeFolder, uniqueFileName);

        if (imageStream.CanSeek)
        {
            imageStream.Seek(0, SeekOrigin.Begin);
        }

        await using var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await imageStream.CopyToAsync(fileStream, cancellationToken);

        // Return relative web URL
        return $"/uploads/invoices/{storeId}/{year}/{uniqueFileName}";
    }

    public async Task<string> SaveTemporaryImageAsync(
        Stream imageStream, 
        string originalFileName, 
        Guid storeId, 
        CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".webp";

        var tempFolder = Path.Combine(_baseUploadDir, "temp", storeId.ToString());
        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }

        var tempKey = Guid.NewGuid().ToString("N");
        var physicalPath = Path.Combine(tempFolder, $"{tempKey}{ext}");

        if (imageStream.CanSeek)
        {
            imageStream.Seek(0, SeekOrigin.Begin);
        }

        await using var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await imageStream.CopyToAsync(fileStream, cancellationToken);

        return $"{tempKey}:{ext}";
    }

    public Task<Stream?> GetTemporaryImageAsync(
        string tempKey, 
        Guid storeId, 
        CancellationToken cancellationToken = default)
    {
        var parts = tempKey.Split(':');
        var key = parts[0];
        var ext = parts.Length > 1 ? parts[1] : ".webp";

        var tempFolder = Path.Combine(_baseUploadDir, "temp", storeId.ToString());
        var physicalPath = Path.Combine(tempFolder, $"{key}{ext}");

        if (!File.Exists(physicalPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task CleanupTemporaryImageAsync(
        string tempKey, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parts = tempKey.Split(':');
            var key = parts[0];
            var ext = parts.Length > 1 ? parts[1] : "*";

            var tempDir = Path.Combine(_baseUploadDir, "temp");
            if (Directory.Exists(tempDir))
            {
                var files = Directory.GetFiles(tempDir, $"{key}.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Suppress cleanup failures to prevent breaking main transaction
        }

        return Task.CompletedTask;
    }
}

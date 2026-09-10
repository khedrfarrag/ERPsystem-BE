using RetailOS.Application.Products.DTOs;

namespace RetailOS.Application.Products.Interfaces;

public interface IProductService
{
    Task<ProductListResponse> GetProductsAsync(
        int page = 1,
        int pageSize = 25,
        string? search = null,
        Guid? categoryId = null,
        bool? isActive = null,
        bool? inStock = null,
        CancellationToken cancellationToken = default);

    Task<ProductResponse> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductResponse> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateProductStatusAsync(Guid id, UpdateProductStatusRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
}

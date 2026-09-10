using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Products.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Catalog;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;

    public ProductService(
        AppDbContext context,
        IStoreContext storeContext,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator)
    {
        _context = context;
        _storeContext = storeContext;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ProductListResponse> GetProductsAsync(
        int page = 1,
        int pageSize = 25,
        string? search = null,
        Guid? categoryId = null,
        bool? isActive = null,
        bool? inStock = null,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Barcode != null && p.Barcode.ToLower().Contains(term)));
        }

        if (inStock.HasValue)
        {
            if (inStock.Value)
            {
                query = query.Where(p => p.InventoryTransactions.Sum(t => (decimal?)t.Quantity) > 0);
            }
            else
            {
                query = query.Where(p => (p.InventoryTransactions.Sum(t => (decimal?)t.Quantity) ?? 0) <= 0);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse(
                p.Id,
                p.Name,
                p.Barcode,
                p.Description,
                p.SellingPrice,
                p.PurchaseCost,
                p.MinStockLevel,
                p.ImageUrl,
                p.IsActive,
                new CategorySummaryDto(p.Category.Id, p.Category.Name),
                new UnitSummaryDto(p.Unit.Id, p.Unit.Name, p.Unit.Symbol),
                p.InventoryTransactions.Sum(t => (decimal?)t.Quantity) ?? 0,
                p.CreatedAt,
                p.UpdatedAt,
                p.WholesalePrice,
                p.IsWholesaleAvailable,
                p.WholesalePrice ?? p.SellingPrice,
                p.CategoryId,
                p.UnitId
            ))
            .ToListAsync(cancellationToken);

        return new ProductListResponse(items, totalCount, page, pageSize);
    }

    public async Task<ProductResponse> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.InventoryTransactions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
            throw new NotFoundException("PRODUCT_NOT_FOUND", "The requested product was not found.");

        return MapToResponse(product);
    }

    public async Task<ProductResponse> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var trimmedBarcode = barcode.Trim();

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.InventoryTransactions)
            .FirstOrDefaultAsync(p => p.Barcode == trimmedBarcode, cancellationToken);

        if (product == null)
            throw new NotFoundException("PRODUCT_NOT_FOUND", "No product found with this barcode.");

        return MapToResponse(product);
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category == null)
            throw new NotFoundException("CATEGORY_NOT_FOUND", "The specified category was not found.");

        if (!category.IsActive)
            throw new DomainException("CATEGORY_INACTIVE", "Cannot assign an inactive category to a product.", 400);

        var unit = await _context.Units
            .FirstOrDefaultAsync(u => u.Id == request.UnitId, cancellationToken);

        if (unit == null)
            throw new NotFoundException("UNIT_NOT_FOUND", "The specified unit was not found.");

        if (!unit.IsActive)
            throw new DomainException("UNIT_INACTIVE", "Cannot assign an inactive unit to a product.", 400);

        var normalizedName = request.Name.Trim().ToLower();
        var nameExists = await _context.Products
            .AnyAsync(p => p.Name.ToLower() == normalizedName, cancellationToken);

        if (nameExists)
            throw new ConflictException("PRODUCT_NAME_CONFLICT", "A product with this name already exists.");

        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var trimmedBarcode = request.Barcode.Trim();
            var barcodeExists = await _context.Products
                .AnyAsync(p => p.Barcode == trimmedBarcode, cancellationToken);

            if (barcodeExists)
                throw new ConflictException("PRODUCT_BARCODE_CONFLICT", "A product with this barcode already exists.");
        }

        var product = new Product
        {
            StoreId = _storeContext.CurrentStoreId!.Value,
            CategoryId = request.CategoryId,
            UnitId = request.UnitId,
            Name = request.Name.Trim(),
            Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim(),
            Description = request.Description?.Trim(),
            SellingPrice = request.SellingPrice,
            WholesalePrice = request.WholesalePrice,
            IsWholesaleAvailable = request.IsWholesaleAvailable,
            PurchaseCost = request.PurchaseCost,
            MinStockLevel = request.MinStockLevel,
            ImageUrl = request.ImageUrl?.Trim(),
            IsActive = true
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        product.Category = category;
        product.Unit = unit;

        return MapToResponse(product);
    }

    public async Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.InventoryTransactions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
            throw new NotFoundException("PRODUCT_NOT_FOUND", "The requested product was not found.");

        if (product.CategoryId != request.CategoryId)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category == null)
                throw new NotFoundException("CATEGORY_NOT_FOUND", "The specified category was not found.");

            if (!category.IsActive)
                throw new DomainException("CATEGORY_INACTIVE", "Cannot assign an inactive category to a product.", 400);

            product.Category = category;
            product.CategoryId = request.CategoryId;
        }

        if (product.UnitId != request.UnitId)
        {
            var unit = await _context.Units
                .FirstOrDefaultAsync(u => u.Id == request.UnitId, cancellationToken);

            if (unit == null)
                throw new NotFoundException("UNIT_NOT_FOUND", "The specified unit was not found.");

            if (!unit.IsActive)
                throw new DomainException("UNIT_INACTIVE", "Cannot assign an inactive unit to a product.", 400);

            product.Unit = unit;
            product.UnitId = request.UnitId;
        }

        var normalizedName = request.Name.Trim().ToLower();
        var nameExists = await _context.Products
            .AnyAsync(p => p.Id != id && p.Name.ToLower() == normalizedName, cancellationToken);

        if (nameExists)
            throw new ConflictException("PRODUCT_NAME_CONFLICT", "A product with this name already exists.");

        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var trimmedBarcode = request.Barcode.Trim();
            var barcodeExists = await _context.Products
                .AnyAsync(p => p.Id != id && p.Barcode == trimmedBarcode, cancellationToken);

            if (barcodeExists)
                throw new ConflictException("PRODUCT_BARCODE_CONFLICT", "A product with this barcode already exists.");
        }

        product.Name = request.Name.Trim();
        product.Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim();
        product.Description = request.Description?.Trim();
        product.SellingPrice = request.SellingPrice;
        product.WholesalePrice = request.WholesalePrice;
        product.IsWholesaleAvailable = request.IsWholesaleAvailable;
        product.PurchaseCost = request.PurchaseCost;
        product.MinStockLevel = request.MinStockLevel;
        product.ImageUrl = request.ImageUrl?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(product);
    }

    public async Task<ProductResponse> UpdateProductStatusAsync(Guid id, UpdateProductStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.InventoryTransactions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
            throw new NotFoundException("PRODUCT_NOT_FOUND", "The requested product was not found.");

        product.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(product);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
            throw new NotFoundException("PRODUCT_NOT_FOUND", "The requested product was not found.");

        var hasTransactions = await _context.InventoryTransactions
            .AnyAsync(t => t.ProductId == id, cancellationToken);

        if (hasTransactions)
            throw new ConflictException("PRODUCT_HAS_TRANSACTIONS", "Product cannot be deleted because inventory transactions exist.");

        product.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static ProductResponse MapToResponse(Product p) => new(
        p.Id,
        p.Name,
        p.Barcode,
        p.Description,
        p.SellingPrice,
        p.PurchaseCost,
        p.MinStockLevel,
        p.ImageUrl,
        p.IsActive,
        new CategorySummaryDto(p.Category?.Id ?? Guid.Empty, p.Category?.Name ?? string.Empty),
        new UnitSummaryDto(p.Unit?.Id ?? Guid.Empty, p.Unit?.Name ?? string.Empty, p.Unit?.Symbol ?? string.Empty),
        p.InventoryTransactions?.Sum(t => t.Quantity) ?? 0,
        p.CreatedAt,
        p.UpdatedAt,
        p.WholesalePrice,
        p.IsWholesaleAvailable,
        p.WholesalePrice ?? p.SellingPrice,
        p.CategoryId,
        p.UnitId
    );
}

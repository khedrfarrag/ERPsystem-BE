using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Categories.Interfaces;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Infrastructure.Persistence;

namespace RetailOS.Infrastructure.Catalog;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;
    private readonly IStoreContext _storeContext;
    private readonly IValidator<CreateCategoryRequest> _createValidator;
    private readonly IValidator<UpdateCategoryRequest> _updateValidator;

    public CategoryService(
        AppDbContext context,
        IStoreContext storeContext,
        IValidator<CreateCategoryRequest> createValidator,
        IValidator<UpdateCategoryRequest> updateValidator)
    {
        _context = context;
        _storeContext = storeContext;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<CategoryListResponse> GetCategoriesAsync(
        int page = 1,
        int pageSize = 25,
        bool? isActive = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Categories.AsNoTracking();

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => MapToResponse(c))
            .ToListAsync(cancellationToken);

        return new CategoryListResponse(items, totalCount, page, pageSize);
    }

    public async Task<CategoryResponse> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
            throw new NotFoundException("CATEGORY_NOT_FOUND", "The requested category was not found.");

        return MapToResponse(category);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var normalizedName = request.Name.Trim().ToLower();
        var exists = await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == normalizedName, cancellationToken);

        if (exists)
            throw new ConflictException("CATEGORY_NAME_CONFLICT", "A category with this name already exists.");

        var category = new Category
        {
            StoreId = _storeContext.CurrentStoreId!.Value,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(category);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
            throw new NotFoundException("CATEGORY_NOT_FOUND", "The requested category was not found.");

        var normalizedName = request.Name.Trim().ToLower();
        var exists = await _context.Categories
            .AnyAsync(c => c.Id != id && c.Name.ToLower() == normalizedName, cancellationToken);

        if (exists)
            throw new ConflictException("CATEGORY_NAME_CONFLICT", "A category with this name already exists.");

        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(category);
    }

    public async Task<CategoryResponse> UpdateCategoryStatusAsync(Guid id, UpdateCategoryStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
            throw new NotFoundException("CATEGORY_NOT_FOUND", "The requested category was not found.");

        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(category);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
            throw new NotFoundException("CATEGORY_NOT_FOUND", "The requested category was not found.");

        var hasProducts = await _context.Products
            .AnyAsync(p => p.CategoryId == id, cancellationToken);

        if (hasProducts)
            throw new ConflictException("CATEGORY_HAS_PRODUCTS", "Category cannot be deleted while it has products assigned.");

        category.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static CategoryResponse MapToResponse(Category c) => new(
        c.Id,
        c.Name,
        c.Description,
        c.IsActive,
        c.CreatedAt,
        c.UpdatedAt
    );
}

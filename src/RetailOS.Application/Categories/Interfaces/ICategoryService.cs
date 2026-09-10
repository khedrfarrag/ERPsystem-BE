using RetailOS.Application.Categories.DTOs;

namespace RetailOS.Application.Categories.Interfaces;

public interface ICategoryService
{
    Task<CategoryListResponse> GetCategoriesAsync(int page = 1, int pageSize = 25, bool? isActive = null, string? search = null, CancellationToken cancellationToken = default);
    Task<CategoryResponse> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse> UpdateCategoryStatusAsync(Guid id, UpdateCategoryStatusRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}

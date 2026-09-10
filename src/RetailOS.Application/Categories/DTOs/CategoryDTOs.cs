namespace RetailOS.Application.Categories.DTOs;

public record CreateCategoryRequest(
    string Name,
    string? Description
);

public record UpdateCategoryRequest(
    string Name,
    string? Description
);

public record UpdateCategoryStatusRequest(
    bool IsActive
);

public record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CategoryListResponse(
    IReadOnlyList<CategoryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);

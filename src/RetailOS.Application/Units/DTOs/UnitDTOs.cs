namespace RetailOS.Application.Units.DTOs;

public record CreateUnitRequest(
    string Name,
    string Symbol,
    string? Description
);

public record UpdateUnitRequest(
    string Name,
    string Symbol,
    string? Description
);

public record UpdateUnitStatusRequest(
    bool IsActive
);

public record UnitResponse(
    Guid Id,
    string Name,
    string Symbol,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record UnitListResponse(
    IReadOnlyList<UnitResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);

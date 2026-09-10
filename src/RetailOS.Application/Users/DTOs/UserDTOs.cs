namespace RetailOS.Application.Users.DTOs;

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Role
);

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Role
);

public record UpdateUserStatusRequest(
    bool IsActive
);

public record UserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    Guid StoreId,
    DateTime CreatedAt
);

public record UserListResponse(
    IReadOnlyList<UserResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);

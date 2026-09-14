namespace RetailOS.Application.Auth.DTOs;

public record RegisterStoreRequest(
    string StoreName,
    string BusinessType,
    string OwnerFirstName,
    string OwnerLastName,
    string Email,
    string Password,
    string? Phone = null,
    string? Address = null
);

public record LoginRequest(
    string Email,
    string Password
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserDto User
);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    Guid StoreId,
    string StoreName,
    bool MustChangePassword = false
);

public record CurrentUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    StoreDto Store,
    bool MustChangePassword = false
);

public record StoreDto(
    Guid Id,
    string Name,
    string BusinessType,
    string? Phone,
    string? Address,
    string Currency,
    string Timezone,
    bool TaxEnabled,
    bool AllowNegativeStock,
    string? InvoicePrefix,
    bool IsActive
);

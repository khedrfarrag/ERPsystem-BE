using RetailOS.Application.Users.DTOs;

namespace RetailOS.Application.Users.Interfaces;

public interface IUserService
{
    Task<UserListResponse> GetUsersAsync(int page = 1, int pageSize = 25, bool? isActive = null, string? role = null, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateUserStatusAsync(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
}

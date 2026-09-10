using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Users.DTOs;
using RetailOS.Application.Users.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Infrastructure.Persistence;
using RetailOS.Shared.Constants;

namespace RetailOS.Infrastructure.Users;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IStoreContext _storeContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;

    public UserService(
        AppDbContext context,
        UserManager<User> userManager,
        IStoreContext storeContext,
        IHttpContextAccessor httpContextAccessor,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator)
    {
        _context = context;
        _userManager = userManager;
        _storeContext = storeContext;
        _httpContextAccessor = httpContextAccessor;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<UserListResponse> GetUsersAsync(
        int page = 1,
        int pageSize = 25,
        bool? isActive = null,
        string? role = null,
        CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Users.AsNoTracking()
            .Where(u => u.Role != Roles.Merchant);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => MapToResponse(u))
            .ToListAsync(cancellationToken);

        return new UserListResponse(items, totalCount, page, pageSize);
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            throw new ConflictException("EMAIL_ALREADY_EXISTS", "A user with this email address already exists.");

        var user = new User
        {
            UserName = request.Email.Trim().ToLowerInvariant(),
            Email = request.Email.Trim().ToLowerInvariant(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Role = request.Role,
            StoreId = _storeContext.CurrentStoreId!.Value,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            throw new DomainException("USER_CREATION_FAILED", "Failed to create user.", 400, errors);
        }

        return MapToResponse(user);
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
            throw new NotFoundException("USER_NOT_FOUND", "The requested user was not found.");

        return MapToResponse(user);
    }

    public async Task<UserResponse> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
            throw new NotFoundException("USER_NOT_FOUND", "The requested user was not found.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Role = request.Role;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(user);
    }

    public async Task<UserResponse> UpdateUserStatusAsync(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!_storeContext.HasStore)
            throw new UnauthorizedException("UNAUTHORIZED", "No tenant context in active session.");

        var currentUserId = GetCurrentUserId();
        if (currentUserId == id && !request.IsActive)
            throw new DomainException("CANNOT_DEACTIVATE_SELF", "An owner cannot deactivate their own account.", 400);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
            throw new NotFoundException("USER_NOT_FOUND", "The requested user was not found.");

        user.IsActive = request.IsActive;

        // If deactivated, revoke all active refresh tokens immediately
        if (!request.IsActive)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapToResponse(user);
    }

    private Guid? GetCurrentUserId()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        var claim = principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                 ?? principal?.FindFirst("sub");

        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    private static UserResponse MapToResponse(User user) => new(
        user.Id,
        user.Email ?? string.Empty,
        user.FirstName,
        user.LastName,
        user.Role,
        user.IsActive,
        user.StoreId,
        user.CreatedAt
    );
}

using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Auth.Interfaces;
using RetailOS.Application.Common.Exceptions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Domain.Entities;
using RetailOS.Infrastructure.Persistence;
using RetailOS.Shared.Constants;

namespace RetailOS.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IStoreContext _storeContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidator<RegisterStoreRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IConfiguration _configuration;

    public AuthService(
        AppDbContext context,
        UserManager<User> userManager,
        IJwtTokenService jwtTokenService,
        IStoreContext storeContext,
        IHttpContextAccessor httpContextAccessor,
        IValidator<RegisterStoreRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _storeContext = storeContext;
        _httpContextAccessor = httpContextAccessor;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterStoreAsync(RegisterStoreRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // Check globally for existing email
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            throw new ConflictException("EMAIL_ALREADY_EXISTS", "A user with this email address already exists.");

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        // 1. Create Store
        var store = new Store
        {
            Name = request.StoreName.Trim(),
            BusinessType = request.BusinessType.Trim(),
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            Currency = "EGP",
            Timezone = "Africa/Cairo",
            TaxEnabled = false,
            AllowNegativeStock = false,
            InvoicePrefix = "INV",
            IsActive = true
        };

        _context.Stores.Add(store);
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Create Owner User
        var user = new User
        {
            UserName = request.Email.Trim().ToLowerInvariant(),
            Email = request.Email.Trim().ToLowerInvariant(),
            FirstName = request.OwnerFirstName.Trim(),
            LastName = request.OwnerLastName.Trim(),
            Role = Roles.Owner,
            StoreId = store.Id,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => e.Description);
            throw new DomainException("USER_CREATION_FAILED", "Failed to create user account.", 400, errors);
        }

        // 3. Issue Tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user, store.Name);
        var (rawRefreshToken, tokenHash) = _jwtTokenService.GenerateRefreshToken();

        var refreshExpiryDays = int.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var exp) ? exp : 7;
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            StoreId = store.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiryDays),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var expiryMinutes = int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out var expMin) ? expMin : 15;

        return new AuthResponse(
            accessToken,
            rawRefreshToken,
            expiryMinutes * 60,
            new UserDto(user.Id, user.Email!, user.FirstName, user.LastName, user.Role, store.Id, store.Name)
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant(), cancellationToken);

        if (user == null)
            throw new UnauthorizedException("INVALID_CREDENTIALS", "Invalid email or password.");

        if (!user.IsActive)
            throw new ForbiddenException("ACCOUNT_DEACTIVATED", "This account has been deactivated. Please contact your store administrator.");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            throw new UnauthorizedException("INVALID_CREDENTIALS", "Invalid email or password.");

        var storeName = user.Store?.Name ?? "Store";
        var accessToken = _jwtTokenService.GenerateAccessToken(user, storeName);
        var (rawRefreshToken, tokenHash) = _jwtTokenService.GenerateRefreshToken();

        var refreshExpiryDays = int.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var exp) ? exp : 7;
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            StoreId = user.StoreId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiryDays),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var expiryMinutes = int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out var expMin) ? expMin : 15;

        return new AuthResponse(
            accessToken,
            rawRefreshToken,
            expiryMinutes * 60,
            new UserDto(user.Id, user.Email!, user.FirstName, user.LastName, user.Role, user.StoreId, storeName)
        );
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedException("INVALID_REFRESH_TOKEN", "Refresh token is required.");

        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);

        var existingToken = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Include(rt => rt.User)
            .ThenInclude(u => u!.Store)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null || existingToken.IsRevoked || existingToken.IsExpired || existingToken.User == null)
            throw new UnauthorizedException("INVALID_REFRESH_TOKEN", "The refresh token is invalid or expired.");

        if (!existingToken.User.IsActive)
            throw new ForbiddenException("ACCOUNT_DEACTIVATED", "This account has been deactivated.");

        // Single-use rotation: Revoke current token
        existingToken.IsRevoked = true;

        var (newRawToken, newTokenHash) = _jwtTokenService.GenerateRefreshToken();
        existingToken.ReplacedByTokenHash = newTokenHash;

        var refreshExpiryDays = int.TryParse(_configuration["Jwt:RefreshTokenExpiryDays"], out var exp) ? exp : 7;
        var newRefreshToken = new RefreshToken
        {
            UserId = existingToken.UserId,
            StoreId = existingToken.StoreId,
            TokenHash = newTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiryDays),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var storeName = existingToken.User.Store?.Name ?? "Store";
        var newAccessToken = _jwtTokenService.GenerateAccessToken(existingToken.User, storeName);
        var expiryMinutes = int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out var expMin) ? expMin : 15;

        return new AuthResponse(
            newAccessToken,
            newRawToken,
            expiryMinutes * 60,
            new UserDto(existingToken.User.Id, existingToken.User.Email!, existingToken.User.FirstName, existingToken.User.LastName, existingToken.User.Role, existingToken.StoreId, storeName)
        );
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return;

        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);
        var existingToken = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken != null && !existingToken.IsRevoked)
        {
            existingToken.IsRevoked = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                       ?? principal?.FindFirst("sub");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedException("UNAUTHORIZED", "User is not authenticated.");

        var user = await _context.Users
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            throw new NotFoundException("USER_NOT_FOUND", "Current user account not found.");

        if (user.Store == null)
            throw new NotFoundException("STORE_NOT_FOUND", "Store associated with this account not found.");

        return new CurrentUserResponse(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Role,
            new StoreDto(
                user.Store.Id,
                user.Store.Name,
                user.Store.BusinessType,
                user.Store.Phone,
                user.Store.Address,
                user.Store.Currency,
                user.Store.Timezone,
                user.Store.TaxEnabled,
                user.Store.AllowNegativeStock,
                user.Store.InvoicePrefix,
                user.Store.IsActive
            )
        );
    }
}

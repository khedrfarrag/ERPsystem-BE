using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RetailOS.Application.Common.Interfaces;

namespace RetailOS.Infrastructure.Services;

public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? CurrentUserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
                return null;

            var claim = user.FindFirst(ClaimTypes.NameIdentifier)
                     ?? user.FindFirst(JwtRegisteredClaimNames.Sub)
                     ?? user.FindFirst("sub");

            if (claim != null && Guid.TryParse(claim.Value, out var userId))
                return userId;

            return null;
        }
    }
}

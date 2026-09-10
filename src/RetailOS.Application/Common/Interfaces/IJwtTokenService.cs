using RetailOS.Domain.Entities;

namespace RetailOS.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, string storeName);
    (string RawToken, string TokenHash) GenerateRefreshToken();
    string HashToken(string token);
}

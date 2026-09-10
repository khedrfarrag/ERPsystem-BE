using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Auth;

public class TokenRotationAndRevocationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TokenRotationAndRevocationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Forwarded-For", "192.168.20.1");
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_RotatesTokenSuccessfully()
    {
        // Arrange
        var email = $"rotate_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest("Store Rotation", "Grocery", "Hany", "Adel", email, "Pass123456!");
        var regResp = await _client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        var oldRefreshToken = auth.RefreshToken;

        // Act
        var refreshResp = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(oldRefreshToken));

        // Assert
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshData = (await refreshResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        refreshData.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshData.RefreshToken.Should().NotBeNullOrWhiteSpace();
        refreshData.RefreshToken.Should().NotBe(oldRefreshToken); // Must be a newly generated token

        // Verify old token is now invalid (single-use rotation)
        var replayResp = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(oldRefreshToken));
        replayResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        // Arrange
        var email = $"logout_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest("Store Logout", "Retail", "Khaled", "Said", email, "Pass123456!");
        var regResp = await _client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        var clientWithAuth = _factory.CreateClient();
        clientWithAuth.DefaultRequestHeaders.Add("X-Forwarded-For", "192.168.20.2");
        clientWithAuth.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Act - logout
        var logoutResp = await clientWithAuth.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest(auth.RefreshToken));
        logoutResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert - refresh with revoked token should fail
        var refreshResp = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(auth.RefreshToken));
        refreshResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

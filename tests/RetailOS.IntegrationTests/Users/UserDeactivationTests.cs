using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Users.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using RetailOS.Shared.Constants;
using Xunit;

namespace RetailOS.IntegrationTests.Users;

public class UserDeactivationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UserDeactivationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.6.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    [Fact]
    public async Task DeactivatedUser_CannotLoginOrRefresh()
    {
        // Arrange - Register store and owner
        var ownerClient = CreateClientWithIp();
        var reg = new RegisterStoreRequest("Deact Store", "Retail", "Owner", "Boss", $"owner_{Guid.NewGuid():N}@t.com", "Pass123456!");
        var regResp = await ownerClient.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Create a Cashier
        var cashierEmail = $"cashier_{Guid.NewGuid():N}@t.com";
        var createReq = new CreateUserRequest("Amr", "Zaki", cashierEmail, "Pass123456!", Roles.Cashier);
        var createResp = await ownerClient.PostAsJsonAsync("/api/users", createReq);
        var cashierUser = (await createResp.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>())!.Data!;

        // Cashier logs in and gets tokens
        var cashierClient = CreateClientWithIp();
        var loginResp = await cashierClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(cashierEmail, "Pass123456!"));
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var cashierAuth = (await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        // Act - Owner deactivates the Cashier
        var deactResp = await ownerClient.PatchAsJsonAsync($"/api/users/{cashierUser.Id}/status", new UpdateUserStatusRequest(false));
        deactResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert 1: Cashier cannot log in anymore
        var loginAfterDeactResp = await cashierClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(cashierEmail, "Pass123456!"));
        loginAfterDeactResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var errorContent = await loginAfterDeactResp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        errorContent!.Code.Should().Be("ACCOUNT_DEACTIVATED");

        // Assert 2: Refresh token is revoked
        var refreshAfterDeactResp = await cashierClient.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(cashierAuth.RefreshToken));
        refreshAfterDeactResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

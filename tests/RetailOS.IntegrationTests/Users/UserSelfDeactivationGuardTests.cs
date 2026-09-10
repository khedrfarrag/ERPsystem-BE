using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Users.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Users;

public class UserSelfDeactivationGuardTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UserSelfDeactivationGuardTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.7.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    [Fact]
    public async Task Owner_CannotDeactivateOwnAccount()
    {
        // Arrange - Register owner
        var client = CreateClientWithIp();
        var email = $"self_deact_{Guid.NewGuid():N}@t.com";
        var reg = new RegisterStoreRequest("Self Deact Guard Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Act - Owner tries to deactivate their own account
        var patchResp = await client.PatchAsJsonAsync($"/api/users/{auth.User.Id}/status", new UpdateUserStatusRequest(false));

        // Assert
        patchResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await patchResp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        error.Should().NotBeNull();
        error!.Code.Should().Be("CANNOT_DEACTIVATE_SELF");
    }
}

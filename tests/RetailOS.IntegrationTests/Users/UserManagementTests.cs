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

public class UserManagementTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UserManagementTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.5.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    [Fact]
    public async Task Owner_CanCreateListAndUpdateUser()
    {
        // Arrange - Register owner
        var client = CreateClientWithIp();
        var email = $"owner_um_{Guid.NewGuid():N}@t.com";
        var reg = new RegisterStoreRequest("User Mgmt Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Act 1: Create a Cashier user
        var cashierEmail = $"cashier_{Guid.NewGuid():N}@t.com";
        var createReq = new CreateUserRequest("Sami", "Yousef", cashierEmail, "TempPass123!", Roles.Cashier);
        var createResp = await client.PostAsJsonAsync("/api/users", createReq);

        // Assert 1
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdUser = (await createResp.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>())!.Data!;
        createdUser.Role.Should().Be(Roles.Cashier);
        createdUser.FirstName.Should().Be("Sami");

        // Act 2: List users
        var listResp = await client.GetAsync("/api/users");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listData = (await listResp.Content.ReadFromJsonAsync<ApiResponse<UserListResponse>>())!.Data!;
        listData.Items.Should().Contain(u => u.Id == createdUser.Id);

        // Act 3: Update user
        var updateReq = new UpdateUserRequest("Samuel", "Yousef", Roles.Manager);
        var updateResp = await client.PutAsJsonAsync($"/api/users/{createdUser.Id}", updateReq);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedUser = (await updateResp.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>())!.Data!;
        updatedUser.FirstName.Should().Be("Samuel");
        updatedUser.Role.Should().Be(Roles.Manager);
    }
}

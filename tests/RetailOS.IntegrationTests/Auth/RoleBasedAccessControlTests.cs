using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Stores.DTOs;
using RetailOS.Domain.Entities;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using RetailOS.Shared.Constants;
using Xunit;

namespace RetailOS.IntegrationTests.Auth;

public class RoleBasedAccessControlTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public RoleBasedAccessControlTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.3.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    [Fact]
    public async Task UpdateStore_ByOwner_IsAllowed()
    {
        // Arrange
        var client = CreateClientWithIp();
        var reg = new RegisterStoreRequest("Store Owner Test", "Retail", "Owner", "One", $"owner_{Guid.NewGuid():N}@t.com", "Pass1234!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Act
        var updateReq = new UpdateStoreRequest("New Owner Store Name", null, null, false, false, "INV");
        var resp = await client.PutAsJsonAsync("/api/stores/current", updateReq);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateStore_ByCashier_IsForbiddenWith403()
    {
        // Arrange - Register store and owner
        var client = CreateClientWithIp();
        var reg = new RegisterStoreRequest("Store Cashier RBAC", "Retail", "Owner", "Boss", $"owner_{Guid.NewGuid():N}@t.com", "Pass1234!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        // Generate a token for a Cashier in this store using IJwtTokenService
        using var scope = _factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var cashierUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "cashier@store.com",
            FirstName = "Cashier",
            LastName = "Employee",
            Role = Roles.Cashier,
            StoreId = auth.User.StoreId,
            IsActive = true
        };

        var cashierToken = jwtService.GenerateAccessToken(cashierUser, auth.User.StoreName);
        var cashierClient = CreateClientWithIp();
        cashierClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cashierToken);

        // Act - Cashier attempts to update store settings (Owner only)
        var updateReq = new UpdateStoreRequest("Hacked Store Name", null, null, false, false, "INV");
        var resp = await cashierClient.PutAsJsonAsync("/api/stores/current", updateReq);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Stores.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Tenant;

public class CrossTenantQueryIsolationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CrossTenantQueryIsolationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.1.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    [Fact]
    public async Task GetCurrentStore_ReturnsOnlyAuthenticatedStoreDetails()
    {
        // Arrange - Register Store A
        var clientA = CreateClientWithIp();
        var emailA = $"storea_{Guid.NewGuid():N}@test.com";
        var regA = new RegisterStoreRequest("Store A", "Grocery", "OwnerA", "User", emailA, "Pass123456!");
        var respA = await clientA.PostAsJsonAsync("/api/auth/register", regA);
        var authA = (await respA.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        // Register Store B
        var clientB = CreateClientWithIp();
        var emailB = $"storeb_{Guid.NewGuid():N}@test.com";
        var regB = new RegisterStoreRequest("Store B", "Pharmacy", "OwnerB", "User", emailB, "Pass123456!");
        var respB = await clientB.PostAsJsonAsync("/api/auth/register", regB);
        var authB = (await respB.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        // Act - Call /api/stores/current using Store A token
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authA.AccessToken);
        var storeRespA = await clientA.GetAsync("/api/stores/current");

        // Act - Call /api/stores/current using Store B token
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authB.AccessToken);
        var storeRespB = await clientB.GetAsync("/api/stores/current");

        // Assert
        storeRespA.StatusCode.Should().Be(HttpStatusCode.OK);
        var dataA = (await storeRespA.Content.ReadFromJsonAsync<ApiResponse<StoreResponse>>())!.Data!;
        dataA.Id.Should().Be(authA.User.StoreId);
        dataA.Name.Should().Be("Store A");

        storeRespB.StatusCode.Should().Be(HttpStatusCode.OK);
        var dataB = (await storeRespB.Content.ReadFromJsonAsync<ApiResponse<StoreResponse>>())!.Data!;
        dataB.Id.Should().Be(authB.User.StoreId);
        dataB.Name.Should().Be("Store B");

        dataA.Id.Should().NotBe(dataB.Id);
    }
}

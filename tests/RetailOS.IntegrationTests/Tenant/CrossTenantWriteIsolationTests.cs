using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Common.Interfaces;
using RetailOS.Application.Stores.DTOs;
using RetailOS.Domain.Entities;
using RetailOS.Infrastructure.Persistence;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Tenant;

public class CrossTenantWriteIsolationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CrossTenantWriteIsolationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.2.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    [Fact]
    public async Task UpdateStore_UpdatesOnlyAuthenticatedStore()
    {
        // Arrange - Register Store 1
        var client1 = CreateClientWithIp();
        var reg1 = new RegisterStoreRequest("Original Store 1", "Grocery", "Owner1", "A", $"s1_{Guid.NewGuid():N}@t.com", "Pass1234!");
        var resp1 = await client1.PostAsJsonAsync("/api/auth/register", reg1);
        var auth1 = (await resp1.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        // Register Store 2
        var client2 = CreateClientWithIp();
        var reg2 = new RegisterStoreRequest("Original Store 2", "Grocery", "Owner2", "B", $"s2_{Guid.NewGuid():N}@t.com", "Pass1234!");
        var resp2 = await client2.PostAsJsonAsync("/api/auth/register", reg2);
        var auth2 = (await resp2.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        // Act - Store 1 updates its name
        client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1.AccessToken);
        var updateReq = new UpdateStoreRequest("Renamed Store 1", "01000000000", "Cairo", false, true, "INV1", "EGP", "Africa/Cairo");
        var updateResp = await client1.PutAsJsonAsync("/api/stores/current", updateReq);

        // Assert
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Store 2 verifies its name is unchanged
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth2.AccessToken);
        var checkResp2 = await client2.GetAsync("/api/stores/current");
        var data2 = (await checkResp2.Content.ReadFromJsonAsync<ApiResponse<StoreResponse>>())!.Data!;
        data2.Name.Should().Be("Original Store 2");
    }

    [Fact]
    public async Task TenantSaveChangesInterceptor_BlocksCrossTenantEntityWrite()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var storeContext = scope.ServiceProvider.GetRequiredService<IStoreContext>();

        var contextStoreId = Guid.NewGuid();
        var attackerStoreId = Guid.NewGuid();

        // Set active tenant in context to contextStoreId
        storeContext.SetCurrentStoreId(contextStoreId);

        // Create an entity with a different (attacker) StoreId
        var rogueRefreshToken = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            StoreId = attackerStoreId, // Mismatched StoreId!
            TokenHash = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        context.RefreshTokens.Add(rogueRefreshToken);

        // Act & Assert - Interceptor must throw InvalidOperationException before hitting DB
        var act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cross-tenant write attempt detected*");
    }
}

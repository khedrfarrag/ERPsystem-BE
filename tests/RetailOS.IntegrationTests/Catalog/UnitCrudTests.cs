using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Catalog;

public class UnitCrudTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UnitCrudTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.12.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterOwnerAsync()
    {
        var client = CreateClientWithIp();
        var email = $"unit_owner_{Guid.NewGuid():N}@t.com";
        var reg = new RegisterStoreRequest("Unit Test Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Unit_Lifecycle_WorksCorrectly()
    {
        var (client, _) = await RegisterOwnerAsync();

        // 1. Create Unit
        var unitName = $"Kilogram_{Guid.NewGuid():N}";
        var unitSymbol = $"kg_{Guid.NewGuid():N}"[..6];
        var createResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest(unitName, unitSymbol, "Weight"));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdUnit = (await createResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;
        createdUnit.Name.Should().Be(unitName);
        createdUnit.Symbol.Should().Be(unitSymbol);

        // 2. Duplicate symbol -> 409 Conflict
        var dupSymbolResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Other_{unitName}", unitSymbol.ToUpperInvariant(), "Dup symbol"));
        dupSymbolResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 3. Duplicate name -> 409 Conflict
        var dupNameResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest(unitName.ToUpperInvariant(), "diff", "Dup name"));
        dupNameResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 4. List units
        var listResp = await client.GetAsync("/api/units");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listData = (await listResp.Content.ReadFromJsonAsync<ApiResponse<UnitListResponse>>())!.Data!;
        listData.Items.Should().Contain(u => u.Id == createdUnit.Id);

        // 5. Update Unit
        var updatedName = $"Updated_{unitName}";
        var updateResp = await client.PutAsJsonAsync($"/api/units/{createdUnit.Id}", new UpdateUnitRequest(updatedName, unitSymbol, "New desc"));
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedUnit = (await updateResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;
        updatedUnit.Name.Should().Be(updatedName);

        // 6. Deactivate Unit
        var deactResp = await client.PatchAsJsonAsync($"/api/units/{createdUnit.Id}/status", new UpdateUnitStatusRequest(false));
        deactResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactUnit = (await deactResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;
        deactUnit.IsActive.Should().BeFalse();

        // 7. Soft Delete Unit
        var delResp = await client.DeleteAsync($"/api/units/{createdUnit.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 8. Verify not found after delete
        var getResp = await client.GetAsync($"/api/units/{createdUnit.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

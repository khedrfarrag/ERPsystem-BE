using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Catalog;

public class ProductValidationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ProductValidationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.14.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth, Guid CategoryId, Guid UnitId)> SetupTestDataAsync()
    {
        var client = CreateClientWithIp();
        var email = $"val_owner_{Guid.NewGuid():N}@t.com";
        var reg = new RegisterStoreRequest("Val Test Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var cat = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unit = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;

        return (client, auth, cat.Id, unit.Id);
    }

    [Fact]
    public async Task CreateProduct_WithZeroSellingPrice_Returns400BadRequest()
    {
        var (client, _, catId, unitId) = await SetupTestDataAsync();

        var req = new CreateProductRequest("Product Zero Price", catId, unitId, 0m);
        var resp = await client.PostAsJsonAsync("/api/products", req);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithNegativePurchaseCost_Returns400BadRequest()
    {
        var (client, _, catId, unitId) = await SetupTestDataAsync();

        var req = new CreateProductRequest("Product Neg Cost", catId, unitId, 10m, null, null, -5m);
        var resp = await client.PostAsJsonAsync("/api/products", req);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithInactiveCategory_Returns400BadRequest()
    {
        var (client, _, catId, unitId) = await SetupTestDataAsync();

        // Deactivate category
        await client.PatchAsJsonAsync($"/api/categories/{catId}/status", new UpdateCategoryStatusRequest(false));

        var req = new CreateProductRequest("Product Inactive Cat", catId, unitId, 10m);
        var resp = await client.PostAsJsonAsync("/api/products", req);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        err!.Code.Should().Be("CATEGORY_INACTIVE");
    }

    [Fact]
    public async Task CreateProduct_WithInactiveUnit_Returns400BadRequest()
    {
        var (client, _, catId, unitId) = await SetupTestDataAsync();

        // Deactivate unit
        await client.PatchAsJsonAsync($"/api/units/{unitId}/status", new UpdateUnitStatusRequest(false));

        var req = new CreateProductRequest("Product Inactive Unit", catId, unitId, 10m);
        var resp = await client.PostAsJsonAsync("/api/products", req);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await resp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        err!.Code.Should().Be("UNIT_INACTIVE");
    }
}

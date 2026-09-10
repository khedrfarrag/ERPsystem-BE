using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Inventory.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Catalog;

public class EntityLifecycleTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public EntityLifecycleTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.18.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterOwnerAsync()
    {
        var client = CreateClientWithIp();
        var email = $"life_owner_{Guid.NewGuid():N}@t.com";
        var reg = new RegisterStoreRequest("Lifecycle Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task DeactivatedCategoryAndUnit_CannotBeUsedOnNewProduct()
    {
        var (client, _) = await RegisterOwnerAsync();

        // 1. Create and deactivate category
        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"DeactCat_{Guid.NewGuid():N}", null));
        var cat = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;
        await client.PatchAsJsonAsync($"/api/categories/{cat.Id}/status", new UpdateCategoryStatusRequest(false));

        // 2. Create active unit
        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"ActiveUnit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unit = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;

        // Attempt product with deactivated category -> 400
        var prodWithDeactCatResp = await client.PostAsJsonAsync("/api/products", new CreateProductRequest("P1", cat.Id, unit.Id, 10m));
        prodWithDeactCatResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errCat = await prodWithDeactCatResp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        errCat!.Code.Should().Be("CATEGORY_INACTIVE");

        // Reactivate category, deactivate unit
        await client.PatchAsJsonAsync($"/api/categories/{cat.Id}/status", new UpdateCategoryStatusRequest(true));
        await client.PatchAsJsonAsync($"/api/units/{unit.Id}/status", new UpdateUnitStatusRequest(false));

        // Attempt product with deactivated unit -> 400
        var prodWithDeactUnitResp = await client.PostAsJsonAsync("/api/products", new CreateProductRequest("P2", cat.Id, unit.Id, 10m));
        prodWithDeactUnitResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errUnit = await prodWithDeactUnitResp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        errUnit!.Code.Should().Be("UNIT_INACTIVE");
    }

    [Fact]
    public async Task Product_WithTransactions_CannotBeDeleted()
    {
        var (client, _) = await RegisterOwnerAsync();

        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var cat = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unit = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;

        var prodResp = await client.PostAsJsonAsync("/api/products", new CreateProductRequest($"Prod_{Guid.NewGuid():N}", cat.Id, unit.Id, 15m));
        var prod = (await prodResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;

        // Record opening stock (creates InventoryTransaction)
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prod.Id, 50m, 10m));

        // Attempt delete product -> 409 Conflict
        var delProdResp = await client.DeleteAsync($"/api/products/{prod.Id}");
        delProdResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var err = await delProdResp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        err!.Code.Should().Be("PRODUCT_HAS_TRANSACTIONS");

        // Attempt delete category linked to product -> 409 Conflict
        var delCatResp = await client.DeleteAsync($"/api/categories/{cat.Id}");
        delCatResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var errCat = await delCatResp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        errCat!.Code.Should().Be("CATEGORY_HAS_PRODUCTS");

        // Attempt delete unit linked to product -> 409 Conflict
        var delUnitResp = await client.DeleteAsync($"/api/units/{unit.Id}");
        delUnitResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var errUnit = await delUnitResp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        errUnit!.Code.Should().Be("UNIT_HAS_PRODUCTS");
    }
}

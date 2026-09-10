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

public class CatalogTenantIsolationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CatalogTenantIsolationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.17.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<HttpClient> RegisterStoreAsync(string storeName, string email)
    {
        var client = CreateClientWithIp();
        var reg = new RegisterStoreRequest(storeName, "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task StoreB_CannotAccess_StoreA_CatalogOrInventory()
    {
        var clientA = await RegisterStoreAsync("Store A", $"store_a_{Guid.NewGuid():N}@t.com");
        var clientB = await RegisterStoreAsync("Store B", $"store_b_{Guid.NewGuid():N}@t.com");

        // 1. Store A creates Category, Unit, Product, Opening Stock
        var catResp = await clientA.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"CatA_{Guid.NewGuid():N}", null));
        var catA = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;

        var unitResp = await clientA.PostAsJsonAsync("/api/units", new CreateUnitRequest($"UnitA_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unitA = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;

        var barcode = $"BAR_{Guid.NewGuid():N}"[..12];
        var prodResp = await clientA.PostAsJsonAsync("/api/products", new CreateProductRequest($"ProdA_{Guid.NewGuid():N}", catA.Id, unitA.Id, 50m, barcode));
        var prodA = (await prodResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;

        await clientA.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prodA.Id, 100m, 20m));

        // 2. Store B attempts to access Store A's Category -> 404
        var getCatResp = await clientB.GetAsync($"/api/categories/{catA.Id}");
        getCatResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 3. Store B attempts to access Store A's Unit -> 404
        var getUnitResp = await clientB.GetAsync($"/api/units/{unitA.Id}");
        getUnitResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 4. Store B attempts to access Store A's Product -> 404
        var getProdResp = await clientB.GetAsync($"/api/products/{prodA.Id}");
        getProdResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 5. Store B attempts barcode lookup of Store A's barcode -> 404
        var getBarcodeResp = await clientB.GetAsync($"/api/products/barcode/{barcode}");
        getBarcodeResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 6. Store B's product list does not contain Store A's product
        var listProdResp = await clientB.GetAsync("/api/products");
        var listProdData = (await listProdResp.Content.ReadFromJsonAsync<ApiResponse<ProductListResponse>>())!.Data!;
        listProdData.Items.Should().NotContain(p => p.Id == prodA.Id);

        // 7. Store B's opening stock list does not contain Store A's entry
        var listStockResp = await clientB.GetAsync("/api/inventory/opening-stock");
        var listStockData = (await listStockResp.Content.ReadFromJsonAsync<ApiResponse<OpeningStockListResponse>>())!.Data!;
        listStockData.Items.Should().NotContain(s => s.ProductId == prodA.Id);
    }
}

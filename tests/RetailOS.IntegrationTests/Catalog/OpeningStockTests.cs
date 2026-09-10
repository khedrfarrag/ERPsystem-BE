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

public class OpeningStockTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public OpeningStockTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.16.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth, Guid Prod1Id, Guid Prod2Id)> SetupOwnerWithProductsAsync()
    {
        var client = CreateClientWithIp();
        var email = $"stock_owner_{Guid.NewGuid():N}@t.com";
        var reg = new RegisterStoreRequest("Stock Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var cat = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unit = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;

        var p1Resp = await client.PostAsJsonAsync("/api/products", new CreateProductRequest($"Prod1_{Guid.NewGuid():N}", cat.Id, unit.Id, 10m));
        var p1 = (await p1Resp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;

        var p2Resp = await client.PostAsJsonAsync("/api/products", new CreateProductRequest($"Prod2_{Guid.NewGuid():N}", cat.Id, unit.Id, 20m));
        var p2 = (await p2Resp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;

        return (client, auth, p1.Id, p2.Id);
    }

    [Fact]
    public async Task SingleOpeningStock_And_UniquenessGuard_WorksCorrectly()
    {
        var (client, _, prod1Id, _) = await SetupOwnerWithProductsAsync();

        // 1. Record opening stock
        var req = new RecordOpeningStockRequest(prod1Id, 150m, 5.25m);
        var resp = await client.PostAsJsonAsync("/api/inventory/opening-stock", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var stockData = (await resp.Content.ReadFromJsonAsync<ApiResponse<OpeningStockResponse>>())!.Data!;
        stockData.Quantity.Should().Be(150m);
        stockData.CostPerUnit.Should().Be(5.25m);
        stockData.TotalValue.Should().Be(150m * 5.25m);

        // 2. Verify currentStock on product
        var prodResp = await client.GetAsync($"/api/products/{prod1Id}");
        prodResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var prodData = (await prodResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        prodData.CurrentStock.Should().Be(150m);

        // 3. Second attempt for same product -> 409 Conflict
        var dupReq = new RecordOpeningStockRequest(prod1Id, 50m, 6.00m);
        var dupResp = await client.PostAsJsonAsync("/api/inventory/opening-stock", dupReq);
        dupResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var dupErr = await dupResp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        dupErr!.Code.Should().Be("OPENING_STOCK_ALREADY_EXISTS");
    }

    [Fact]
    public async Task BulkOpeningStock_WorksCorrectly()
    {
        var (client, _, prod1Id, prod2Id) = await SetupOwnerWithProductsAsync();

        // Pre-record opening stock for prod1
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prod1Id, 100m, 4m));

        // Bulk request with prod1 (already exists) and prod2 (new)
        var bulkReq = new BulkOpeningStockRequest(new List<OpeningStockEntryRequest>
        {
            new(prod1Id, 50m, 5m),
            new(prod2Id, 200m, 12.50m)
        });

        var bulkResp = await client.PostAsJsonAsync("/api/inventory/opening-stock/bulk", bulkReq);
        bulkResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var bulkData = (await bulkResp.Content.ReadFromJsonAsync<ApiResponse<BulkOpeningStockResponse>>())!.Data!;

        bulkData.TotalRequested.Should().Be(2);
        bulkData.Created.Should().Be(1);
        bulkData.SkippedAlreadyExists.Should().Be(1);
        bulkData.Errors.Should().Contain(e => e.ProductId == prod1Id && e.Code == "OPENING_STOCK_ALREADY_EXISTS");

        // Verify prod2 current stock
        var p2Resp = await client.GetAsync($"/api/products/{prod2Id}");
        var p2Data = (await p2Resp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        p2Data.CurrentStock.Should().Be(200m);

        // List opening stock entries
        var listResp = await client.GetAsync("/api/inventory/opening-stock");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listData = (await listResp.Content.ReadFromJsonAsync<ApiResponse<OpeningStockListResponse>>())!.Data!;
        listData.Items.Should().Contain(e => e.ProductId == prod2Id);
    }
}

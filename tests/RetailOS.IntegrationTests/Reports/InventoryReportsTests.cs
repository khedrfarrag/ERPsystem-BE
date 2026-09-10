using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Inventory.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Reports.DTOs;
using RetailOS.Application.Sales.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Reports;

public class InventoryReportsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public InventoryReportsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.42.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterStoreAsync()
    {
        var client = CreateClientWithIp();
        var email = $"rep_inv_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest("Inventory Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Inventory_Valuation_Stock_Movement_And_Low_Stock_Alerts_Work()
    {
        var (client, _) = await RegisterStoreAsync();

        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var catId = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!.Id;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unitId = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!.Id;

        // Product A: 10 units opening @ 10, Min Reorder Level 15 (should trigger alert!)
        var prodA = (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest($"ProdA_{Guid.NewGuid():N}", catId, unitId, 25m, MinStockLevel: 15m))).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prodA.Id, 10m, 10m));

        // Product B: 20 units opening @ 20, Min Reorder Level 5 (no alert)
        var prodB = (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest($"ProdB_{Guid.NewGuid():N}", catId, unitId, 40m, MinStockLevel: 5m))).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prodB.Id, 20m, 20m));

        // 1. Valuation Check: 10*10 + 20*20 = 100 + 400 = 500
        var valResp = await client.GetAsync("/api/reports/inventory/valuation");
        valResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var valuation = (await valResp.Content.ReadFromJsonAsync<ApiResponse<InventoryValuationReportResponse>>())!.Data!;
        valuation.TotalValuation.Should().Be(500m);
        valuation.TotalUnitsCount.Should().Be(30m);

        // 2. CSV Export of Valuation
        var valCsvResp = await client.GetAsync("/api/reports/inventory/valuation?format=csv");
        valCsvResp.StatusCode.Should().Be(HttpStatusCode.OK);
        valCsvResp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");

        // 3. Stock Movement on Product A:
        // Sell 3 units -> Remaining 7
        var saleResp = await client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(null, "Cash", 0m, "Sale", new List<SaleLineItemRequest> { new(prodA.Id, 3m, 25m) }));
        saleResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale = (await saleResp.Content.ReadFromJsonAsync<ApiResponse<SaleResponse>>())!.Data!;

        // Return 1 unit -> Remaining 8
        await client.PostAsJsonAsync($"/api/sales/{sale.Id}/returns", new CreateSaleReturnRequest("Return", "Cash", new List<SaleReturnItemRequest> { new(prodA.Id, 1m) }));

        // Check Movement Ledger for Product A
        var movResp = await client.GetAsync($"/api/reports/inventory/movement/{prodA.Id}");
        movResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var movement = (await movResp.Content.ReadFromJsonAsync<ApiResponse<ProductStockMovementReportResponse>>())!.Data!;
        movement.CurrentStock.Should().Be(8m);
        movement.Movements.Should().HaveCount(3); // OpeningBalance, Sale, SaleReturn

        // 4. Low Stock Alerts:
        // ProdA has stock 8 <= 15 (reorder level) -> shortage 7
        // ProdB has stock 20 > 5 -> should NOT appear in alerts
        var alertResp = await client.GetAsync("/api/reports/inventory/low-stock");
        alertResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var alerts = (await alertResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<LowStockAlertItemResponse>>>())!.Data!;
        alerts.Should().ContainSingle(a => a.ProductId == prodA.Id);
        alerts.Should().NotContain(a => a.ProductId == prodB.Id);
        alerts[0].ShortageQuantity.Should().Be(7m);
    }
}

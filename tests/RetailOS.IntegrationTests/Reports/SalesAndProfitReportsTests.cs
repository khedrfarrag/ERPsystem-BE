using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Expenses.DTOs;
using RetailOS.Application.Inventory.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Reports.DTOs;
using RetailOS.Application.Sales.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Reports;

public class SalesAndProfitReportsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SalesAndProfitReportsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.41.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterStoreAsync()
    {
        var client = CreateClientWithIp();
        var email = $"rep_sp_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest("Sales & Profit Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Sales_Summary_And_Profit_Loss_Calculation_Accurate()
    {
        var (client, _) = await RegisterStoreAsync();

        // 1. Setup Category, Unit, Products with Stock
        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var catId = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!.Id;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unitId = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!.Id;

        // Product 1: Cost 10, Price 25
        var prod1 = (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest($"Prod1_{Guid.NewGuid():N}", catId, unitId, 25m))).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prod1.Id, 100m, 10m));

        // Product 2: Cost 50, Price 100
        var prod2 = (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest($"Prod2_{Guid.NewGuid():N}", catId, unitId, 100m))).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prod2.Id, 50m, 50m));

        // 2. Sales:
        // Sale 1: 2 units of Prod1 = 50 Cash (Cost: 20)
        var sale1Resp = await client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(null, "Cash", 0m, "Sale 1", new List<SaleLineItemRequest> { new(prod1.Id, 2m, 25m) }));
        sale1Resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var sale1 = (await sale1Resp.Content.ReadFromJsonAsync<ApiResponse<SaleResponse>>())!.Data!;

        // Sale 2: 1 unit of Prod2 = 100 Cash (Cost: 50)
        var sale2Resp = await client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(null, "Cash", 0m, "Sale 2", new List<SaleLineItemRequest> { new(prod2.Id, 1m, 100m) }));
        sale2Resp.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Return 1 unit of Prod1 from Sale 1 = 25 Refund (Cost: 10)
        var retResp = await client.PostAsJsonAsync($"/api/sales/{sale1.Id}/returns", new CreateSaleReturnRequest("Customer return", "Cash", new List<SaleReturnItemRequest> { new(prod1.Id, 1m) }));
        retResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // 4. Record Operating Expense: 20
        var expCatResp = await client.PostAsJsonAsync("/api/expenses/categories", new CreateExpenseCategoryRequest($"ExpCat_{Guid.NewGuid():N}"));
        var expCatId = (await expCatResp.Content.ReadFromJsonAsync<ApiResponse<ExpenseCategoryResponse>>())!.Data!.Id;
        await client.PostAsJsonAsync("/api/expenses", new CreateExpenseRequest(expCatId, 20m, DateTimeOffset.UtcNow, "Cash", "Shop supplies"));

        // --- VERIFY SALES SUMMARY REPORT ---
        var salesReportResp = await client.GetAsync("/api/reports/sales");
        salesReportResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var salesReport = (await salesReportResp.Content.ReadFromJsonAsync<ApiResponse<SalesSummaryReportResponse>>())!.Data!;

        // Gross = 50 + 100 = 150, Returns = 25, Net = 125
        salesReport.GrossSales.Should().Be(150m);
        salesReport.TotalReturns.Should().Be(25m);
        salesReport.NetSales.Should().Be(125m);
        salesReport.TotalOrders.Should().Be(2);

        // Top products contains both Prod1 and Prod2
        salesReport.TopProducts.Should().HaveCount(2);

        // --- VERIFY CSV EXPORT ---
        var csvResp = await client.GetAsync("/api/reports/sales?format=csv");
        csvResp.StatusCode.Should().Be(HttpStatusCode.OK);
        csvResp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var csvText = await csvResp.Content.ReadAsStringAsync();
        csvText.Should().Contain("Gross Sales,150");
        csvText.Should().Contain("Net Sales,125");

        // --- VERIFY PROFIT & LOSS (P&L) REPORT ---
        var plResp = await client.GetAsync("/api/reports/profit-loss");
        plResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var pl = (await plResp.Content.ReadFromJsonAsync<ApiResponse<ProfitLossReportResponse>>())!.Data!;

        // Revenue: Net Sales = 125
        // COGS: Prod1 (2 - 1 = 1 unit @ 10) + Prod2 (1 unit @ 50) = 60
        // Gross Profit = 125 - 60 = 65 (Margin: 65/125 = 52%)
        // Expenses = 20
        // Net Profit = 65 - 20 = 45 (Margin: 45/125 = 36%)
        pl.NetSalesRevenue.Should().Be(125m);
        pl.CostOfGoodsSold.Should().Be(60m);
        pl.GrossProfit.Should().Be(65m);
        pl.GrossProfitMarginPercentage.Should().Be(52m);
        pl.OperatingExpenses.Should().Be(20m);
        pl.NetProfit.Should().Be(45m);
        pl.NetProfitMarginPercentage.Should().Be(36m);
    }
}

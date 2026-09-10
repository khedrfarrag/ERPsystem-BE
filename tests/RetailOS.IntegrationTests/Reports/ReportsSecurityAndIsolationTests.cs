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
using RetailOS.Application.Users.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Reports;

public class ReportsSecurityAndIsolationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ReportsSecurityAndIsolationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.44.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterStoreAsync(string name)
    {
        var client = CreateClientWithIp();
        var email = $"sec_rep_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest(name, "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Cashier_Forbidden_From_All_Financial_Reports()
    {
        var (ownerClient, _) = await RegisterStoreAsync("Secured Store");

        // Create Cashier user
        var cashierEmail = $"rep_cashier_{Guid.NewGuid():N}@test.com";
        await ownerClient.PostAsJsonAsync("/api/users", new CreateUserRequest("Cashier", "Sam", cashierEmail, "Pass123456!", "Cashier"));

        // Login as Cashier
        var cashierClient = CreateClientWithIp();
        var loginResp = await cashierClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(cashierEmail, "Pass123456!"));
        var cashierAuth = (await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        cashierClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cashierAuth.AccessToken);

        // Verify 403 on financial report endpoints
        (await cashierClient.GetAsync("/api/reports/sales")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await cashierClient.GetAsync("/api/reports/profit-loss")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await cashierClient.GetAsync("/api/reports/inventory/valuation")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await cashierClient.GetAsync("/api/reports/balances/customers")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await cashierClient.GetAsync("/api/reports/balances/suppliers")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await cashierClient.GetAsync("/api/reports/cash-register")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StoreA_Report_Data_Is_Completely_Isolated_From_StoreB()
    {
        var (clientA, _) = await RegisterStoreAsync("Store Alpha");
        var (clientB, _) = await RegisterStoreAsync("Store Beta");

        // Store A creates product and sale of $500
        var catA = (await (await clientA.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_A_{Guid.NewGuid():N}", null))).Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;
        var unitA = (await (await clientA.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_A_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null))).Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;
        var prodA = (await (await clientA.PostAsJsonAsync("/api/products", new CreateProductRequest($"Prod_A_{Guid.NewGuid():N}", catA.Id, unitA.Id, 500m))).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        await clientA.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prodA.Id, 10m, 200m));
        await clientA.PostAsJsonAsync("/api/sales", new CreateSaleRequest(null, "Cash", 0m, "Store A Sale", new List<SaleLineItemRequest> { new(prodA.Id, 1m, 500m) }));

        // Store B queries sales report -> should have 0 sales and 0 revenue!
        var repB = (await (await clientB.GetAsync("/api/reports/sales")).Content.ReadFromJsonAsync<ApiResponse<SalesSummaryReportResponse>>())!.Data!;
        repB.GrossSales.Should().Be(0m);
        repB.NetSales.Should().Be(0m);
        repB.TotalOrders.Should().Be(0);

        // Store B queries inventory valuation -> should have 0 products and $0 valuation!
        var valB = (await (await clientB.GetAsync("/api/reports/inventory/valuation")).Content.ReadFromJsonAsync<ApiResponse<InventoryValuationReportResponse>>())!.Data!;
        valB.TotalValuation.Should().Be(0m);
        valB.TotalProductsCount.Should().Be(0);
    }
}

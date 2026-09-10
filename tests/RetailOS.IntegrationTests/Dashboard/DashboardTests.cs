using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.CashRegister.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Customers.DTOs;
using RetailOS.Application.Dashboard.DTOs;
using RetailOS.Application.Expenses.DTOs;
using RetailOS.Application.Inventory.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Sales.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.Application.Users.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using RetailOS.Shared.Constants;
using Xunit;

namespace RetailOS.IntegrationTests.Dashboard;

public class DashboardTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DashboardTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.55.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterStoreAsync(string storeName = "Dashboard Test Store")
    {
        var client = CreateClientWithIp();
        var email = $"dash_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest(storeName, "Retail", "Owner", "Admin", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Dashboard_Summary_Returns_Accurate_Calculations()
    {
        var (client, _) = await RegisterStoreAsync();

        // 1. Setup Category, Unit, Products
        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var catId = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!.Id;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unitId = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!.Id;

        // Prod1: Selling 50, Cost 20, MinStockLevel = 10
        var prod1 = (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"Prod1_{Guid.NewGuid():N}", catId, unitId, 50m, null, null, 20m, 10m, null))).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prod1.Id, 20m, 20m));

        // 2. Open Cash Register with float 300
        var openResp = await client.PostAsJsonAsync("/api/cash-register/open", new OpenFloatRequest(300m, "Morning float"));
        openResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Create Customer and Mixed Sale (Total 100, Cash 40, Credit 60)
        var cust = (await (await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest($"Cust_{Guid.NewGuid():N}", "01011112222", null, 1000m, null, null))).Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data!;
        
        var saleReq = new CreateSaleRequest(
            CustomerId: cust.Id,
            PaymentMethod: "Mixed",
            CashAmount: 40m,
            Notes: "Test Sale",
            Items: new List<SaleLineItemRequest> { new SaleLineItemRequest(prod1.Id, 2m, 50m, 0m) }
        );
        var saleResp = await client.PostAsJsonAsync("/api/sales", saleReq);
        saleResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // 4. Record Expense 20
        var expCat = (await (await client.PostAsJsonAsync("/api/expenses/categories", new CreateExpenseCategoryRequest($"ExpCat_{Guid.NewGuid():N}"))).Content.ReadFromJsonAsync<ApiResponse<ExpenseCategoryResponse>>())!.Data!;
        await client.PostAsJsonAsync("/api/expenses", new CreateExpenseRequest(expCat.Id, 20m, DateTimeOffset.UtcNow, "Cash", "Electric bill"));

        // 5. Query Dashboard Summary
        var summaryResp = await client.GetAsync("/api/dashboard/summary");
        summaryResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = (await summaryResp.Content.ReadFromJsonAsync<ApiResponse<DashboardSummaryDto>>())!.Data!;

        summary.TodaySalesRevenue.Should().Be(100m);
        summary.TodayOrdersCount.Should().Be(1);
        summary.TodayCashSales.Should().Be(40m);
        summary.TodayCreditSales.Should().Be(60m);
        // Gross Profit = 100 - (2 * 20) = 60
        summary.TodayGrossProfit.Should().Be(60m);
        summary.TodayExpenses.Should().Be(20m);
        // Operating Profit = 60 - 20 = 40
        summary.TodayOperatingProfit.Should().Be(40m);
        summary.IsCashRegisterOpen.Should().BeTrue();
        // Live Drawer Cash = Float (300) + Cash Sale (40) - Cash Expense (20) = 320
        summary.LiveCashDrawerBalance.Should().Be(320m);
        summary.TotalReceivables.Should().Be(60m);
    }

    [Fact]
    public async Task Sales_Trend_Returns_Zero_Filled_Consecutive_Days()
    {
        var (client, _) = await RegisterStoreAsync();

        var trendResp = await client.GetAsync("/api/dashboard/sales-trend?days=7");
        trendResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var trend = (await trendResp.Content.ReadFromJsonAsync<ApiResponse<SalesTrendDto>>())!.Data!;
        trend.TotalDays.Should().Be(7);
        trend.Points.Should().HaveCount(7);

        // Check date chronological progression
        for (int i = 0; i < trend.Points.Count - 1; i++)
        {
            var d1 = DateTime.Parse(trend.Points[i].Date);
            var d2 = DateTime.Parse(trend.Points[i + 1].Date);
            d2.Should().Be(d1.AddDays(1));
        }
    }

    [Fact]
    public async Task Top_And_Slow_Moving_Products_Identified_Accurately()
    {
        var (client, _) = await RegisterStoreAsync();

        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var catId = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!.Id;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unitId = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!.Id;

        // Prod A: Fast seller
        var prodA = (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"ProdFast_{Guid.NewGuid():N}", catId, unitId, 40m, null, null, 15m, 5m, null))).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prodA.Id, 50m, 15m));

        // Prod B: Slow moving (Stock on hand, 0 sales)
        var prodB = (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"ProdSlow_{Guid.NewGuid():N}", catId, unitId, 100m, null, null, 60m, 2m, null))).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prodB.Id, 10m, 60m));

        // Sell 5 units of Prod A
        await client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(
            CustomerId: null,
            PaymentMethod: "Cash",
            CashAmount: 200m,
            Notes: null,
            Items: new List<SaleLineItemRequest> { new SaleLineItemRequest(prodA.Id, 5m, 40m, 0m) }
        ));

        // 1. Check Top Products
        var topResp = await client.GetAsync("/api/dashboard/top-products?days=30&limit=5");
        topResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var topProducts = (await topResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TopProductDto>>>())!.Data!;
        topProducts.Should().ContainSingle(p => p.ProductId == prodA.Id);
        topProducts.First(p => p.ProductId == prodA.Id).QuantitySold.Should().Be(5m);

        // 2. Check Slow-Moving Products
        var slowResp = await client.GetAsync("/api/dashboard/slow-moving-products?days=30&limit=10");
        slowResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var slowProducts = (await slowResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SlowMovingProductDto>>>())!.Data!;
        slowProducts.Should().Contain(p => p.ProductId == prodB.Id);
        var stagnantItem = slowProducts.First(p => p.ProductId == prodB.Id);
        stagnantItem.CurrentStock.Should().Be(10m);
        stagnantItem.TiedUpCapital.Should().Be(600m); // 10 * 60
    }

    [Fact]
    public async Task Low_Stock_Alerts_And_Recent_Activity_Work()
    {
        var (client, _) = await RegisterStoreAsync();

        var cat = (await (await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null))).Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;
        var unit = (await (await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null))).Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;

        // Product with MinStockLevel = 15, current stock = 5
        var prod = (await (await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"LowProd_{Guid.NewGuid():N}", cat.Id, unit.Id, 30m, null, null, 10m, 15m, null))).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prod.Id, 5m, 10m));

        // Check Low-Stock Alerts
        var alertResp = await client.GetAsync("/api/dashboard/low-stock-alerts");
        alertResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var alerts = (await alertResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<LowStockAlertDto>>>())!.Data!;
        alerts.Should().Contain(a => a.ProductId == prod.Id);
        var alert = alerts.First(a => a.ProductId == prod.Id);
        alert.CurrentStock.Should().Be(5m);
        alert.MinStockLevel.Should().Be(15m);
        alert.DeficitQuantity.Should().Be(10m);

        // Check Recent Activity
        var actResp = await client.GetAsync("/api/dashboard/recent-activity");
        actResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var acts = (await actResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<RecentActivityDto>>>())!.Data!;
        acts.Should().NotBeNull();
    }

    [Fact]
    public async Task Cashier_Role_Forbidden_On_Dashboard_Endpoints()
    {
        var (ownerClient, _) = await RegisterStoreAsync();

        // Create Cashier user
        var cashierEmail = $"cashier_{Guid.NewGuid():N}@test.com";
        var userResp = await ownerClient.PostAsJsonAsync("/api/users", new CreateUserRequest("Sami", "Cashier", cashierEmail, "Pass123456!", Roles.Cashier));
        userResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Login as Cashier
        var loginResp = await ownerClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(cashierEmail, "Pass123456!"));
        var cashierAuth = (await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        var cashierClient = CreateClientWithIp();
        cashierClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cashierAuth.AccessToken);

        // Assert 403 Forbidden on dashboard endpoints
        var r1 = await cashierClient.GetAsync("/api/dashboard/summary");
        r1.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var r2 = await cashierClient.GetAsync("/api/dashboard/sales-trend");
        r2.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var r3 = await cashierClient.GetAsync("/api/dashboard/top-products");
        r3.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Multi_Tenant_Isolation_Ensured_Between_Stores()
    {
        var (clientA, _) = await RegisterStoreAsync("Store A");
        var (clientB, _) = await RegisterStoreAsync("Store B");

        // Record expense in Store A
        var expCatA = (await (await clientA.PostAsJsonAsync("/api/expenses/categories", new CreateExpenseCategoryRequest($"ExpCat_{Guid.NewGuid():N}"))).Content.ReadFromJsonAsync<ApiResponse<ExpenseCategoryResponse>>())!.Data!;
        await clientA.PostAsJsonAsync("/api/expenses", new CreateExpenseRequest(expCatA.Id, 750m, DateTimeOffset.UtcNow, "Cash", "Store A Expense"));

        // Store B summary should show 0 expenses
        var summaryB = (await (await clientB.GetAsync("/api/dashboard/summary")).Content.ReadFromJsonAsync<ApiResponse<DashboardSummaryDto>>())!.Data!;
        summaryB.TodayExpenses.Should().Be(0m);
        summaryB.TodaySalesRevenue.Should().Be(0m);
    }
}

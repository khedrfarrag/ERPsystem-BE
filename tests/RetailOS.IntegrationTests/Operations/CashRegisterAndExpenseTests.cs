using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.CashRegister.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Expenses.DTOs;
using RetailOS.Application.Inventory.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Sales.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.Application.Users.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Operations;

public class CashRegisterAndExpenseTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CashRegisterAndExpenseTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.35.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterStoreAsync()
    {
        var client = CreateClientWithIp();
        var email = $"cash_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest("Cash Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Expense_And_Cash_Register_EOD_Reconciliation_Works()
    {
        var (client, _) = await RegisterStoreAsync();

        // 1. Morning Opening Float of 500
        var openFloatResp = await client.PostAsJsonAsync("/api/cash-register/open", new OpenFloatRequest(500m, "Morning float"));
        openFloatResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Setup Category & Product with Stock for a Cash Sale
        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var catId = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!.Id;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unitId = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!.Id;

        var prodResp = await client.PostAsJsonAsync("/api/products", new CreateProductRequest($"Prod_{Guid.NewGuid():N}", catId, unitId, 200m));
        var prodId = (await prodResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!.Id;

        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prodId, 10m, 100m));

        // 3. Make Cash Sale: 1 unit at 200
        var saleResp = await client.PostAsJsonAsync("/api/sales", new CreateSaleRequest(null, "Cash", 0m, "Cash item", new List<SaleLineItemRequest> { new(prodId, 1m, 200m) }));
        saleResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Current expected cash = 500 (float) + 200 (sale) = 700

        // 4. Create Expense: 150 Cash for Utilities
        var expCatResp = await client.PostAsJsonAsync("/api/expenses/categories", new CreateExpenseCategoryRequest($"Utilities_{Guid.NewGuid():N}"));
        expCatResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var expCatId = (await expCatResp.Content.ReadFromJsonAsync<ApiResponse<ExpenseCategoryResponse>>())!.Data!.Id;

        var expResp = await client.PostAsJsonAsync("/api/expenses", new CreateExpenseRequest(expCatId, 150m, DateTimeOffset.UtcNow, "Cash", "Electricity bill"));
        expResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Current expected cash = 700 - 150 = 550
        var summaryResp = await client.GetAsync("/api/cash-register/current");
        summaryResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = (await summaryResp.Content.ReadFromJsonAsync<ApiResponse<CashRegisterSummaryResponse>>())!.Data!;
        summary.CurrentBalance.Should().Be(550m);
        summary.LastFloatAmount.Should().Be(500m);

        // 5. End of Day Reconciliation: Counted 540 (-10 discrepancy)
        var closeResp = await client.PostAsJsonAsync("/api/cash-register/close", new CloseRegisterRequest(540m, "10 short"));
        closeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var closeData = (await closeResp.Content.ReadFromJsonAsync<ApiResponse<CashRegisterCloseResponse>>())!.Data!;
        closeData.ExpectedBalance.Should().Be(550m);
        closeData.CountedAmount.Should().Be(540m);
        closeData.Discrepancy.Should().Be(-10m);

        // Current balance after adjustment should now be 540
        var finalSummary = (await (await client.GetAsync("/api/cash-register/current")).Content.ReadFromJsonAsync<ApiResponse<CashRegisterSummaryResponse>>())!.Data!;
        finalSummary.CurrentBalance.Should().Be(540m);
    }

    [Fact]
    public async Task Cashier_Forbidden_From_Expenses_And_Float_Configuration()
    {
        var (client, _) = await RegisterStoreAsync();

        // Create Cashier user
        var cashierEmail = $"cashier_{Guid.NewGuid():N}@test.com";
        var createCashierResp = await client.PostAsJsonAsync("/api/users", new CreateUserRequest("Cashier", "Sam", cashierEmail, "Pass123456!", "Cashier"));
        createCashierResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Login as Cashier
        var loginClient = CreateClientWithIp();
        var loginResp = await loginClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(cashierEmail, "Pass123456!"));
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var cashierAuth = (await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        loginClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cashierAuth.AccessToken);

        // Cashier attempts to open float -> 403 Forbidden
        var openResp = await loginClient.PostAsJsonAsync("/api/cash-register/open", new OpenFloatRequest(100m));
        openResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Cashier attempts to create expense category -> 403 Forbidden
        var expCatResp = await loginClient.PostAsJsonAsync("/api/expenses/categories", new CreateExpenseCategoryRequest("Disallowed"));
        expCatResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Cashier IS allowed to read current summary
        var readSummaryResp = await loginClient.GetAsync("/api/cash-register/current");
        readSummaryResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

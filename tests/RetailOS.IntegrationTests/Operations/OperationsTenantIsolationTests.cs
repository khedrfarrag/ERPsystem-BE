using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.CashRegister.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Customers.DTOs;
using RetailOS.Application.Expenses.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Purchases.DTOs;
using RetailOS.Application.Sales.DTOs;
using RetailOS.Application.Suppliers.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Operations;

public class OperationsTenantIsolationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public OperationsTenantIsolationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.36.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterStoreAsync(string storeName)
    {
        var client = CreateClientWithIp();
        var email = $"iso_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest(storeName, "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task StoreA_Operations_Are_Completely_Invisible_To_StoreB()
    {
        var (clientA, _) = await RegisterStoreAsync("Store Alpha");
        var (clientB, _) = await RegisterStoreAsync("Store Beta");

        // 1. Store A creates Supplier
        var supAResp = await clientA.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest($"Sup_A_{Guid.NewGuid():N}", "+201011112222"));
        var supAId = (await supAResp.Content.ReadFromJsonAsync<ApiResponse<SupplierResponse>>())!.Data!.Id;

        // 2. Store A creates Customer
        var cusAResp = await clientA.PostAsJsonAsync("/api/customers", new CreateCustomerRequest($"Cus_A_{Guid.NewGuid():N}", "+201022223333"));
        var cusAId = (await cusAResp.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data!.Id;

        // 3. Store A creates Product & Purchase
        var catA = (await (await clientA.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_A_{Guid.NewGuid():N}", null))).Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;
        var unitA = (await (await clientA.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_A_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null))).Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;
        var prodA = (await (await clientA.PostAsJsonAsync("/api/products", new CreateProductRequest($"Prod_A_{Guid.NewGuid():N}", catA.Id, unitA.Id, 100m))).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;

        var poAResp = await clientA.PostAsJsonAsync("/api/purchases", new CreatePurchaseRequest(supAId, "INV-A", DateTimeOffset.UtcNow, null, new List<PurchaseLineItemRequest> { new(prodA.Id, 10m, 50m) }));
        var poAId = (await poAResp.Content.ReadFromJsonAsync<ApiResponse<PurchaseResponse>>())!.Data!.Id;

        // Confirm purchase so stock is available for sale
        await clientA.PostAsync($"/api/purchases/{poAId}/confirm", null);

        // 4. Store A creates Sale
        var saleAResp = await clientA.PostAsJsonAsync("/api/sales", new CreateSaleRequest(null, "Cash", 0m, "Sale A", new List<SaleLineItemRequest> { new(prodA.Id, 1m, 100m) }));
        var saleAId = (await saleAResp.Content.ReadFromJsonAsync<ApiResponse<SaleResponse>>())!.Data!.Id;

        // 5. Store A creates Expense
        var expCatA = (await (await clientA.PostAsJsonAsync("/api/expenses/categories", new CreateExpenseCategoryRequest($"ExpCat_A_{Guid.NewGuid():N}"))).Content.ReadFromJsonAsync<ApiResponse<ExpenseCategoryResponse>>())!.Data!;
        await clientA.PostAsJsonAsync("/api/expenses", new CreateExpenseRequest(expCatA.Id, 50m, DateTimeOffset.UtcNow, "Cash"));

        // Store A opens float
        await clientA.PostAsJsonAsync("/api/cash-register/open", new OpenFloatRequest(1000m));

        // --- VERIFICATIONS FOR STORE B ---

        // Store B should see empty list of suppliers
        var supBList = (await (await clientB.GetAsync("/api/suppliers")).Content.ReadFromJsonAsync<ApiResponse<SupplierListResponse>>())!.Data!;
        supBList.Items.Should().NotContain(s => s.Id == supAId);

        // Store B direct query for Store A's supplier should return 404
        var supBDirect = await clientB.GetAsync($"/api/suppliers/{supAId}");
        supBDirect.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Store B direct query for Store A's customer should return 404
        var cusBDirect = await clientB.GetAsync($"/api/customers/{cusAId}");
        cusBDirect.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Store B direct query for Store A's purchase should return 404
        var poBDirect = await clientB.GetAsync($"/api/purchases/{poAId}");
        poBDirect.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Store B direct query for Store A's sale should return 404
        var saleBDirect = await clientB.GetAsync($"/api/sales/{saleAId}");
        saleBDirect.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Store B cash register summary should be completely independent (0 balance, no last float)
        var cashBSummary = (await (await clientB.GetAsync("/api/cash-register/current")).Content.ReadFromJsonAsync<ApiResponse<CashRegisterSummaryResponse>>())!.Data!;
        cashBSummary.CurrentBalance.Should().Be(0m);
        cashBSummary.LastFloatAmount.Should().BeNull();
    }
}

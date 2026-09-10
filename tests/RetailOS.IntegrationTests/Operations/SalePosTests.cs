using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Customers.DTOs;
using RetailOS.Application.Inventory.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Sales.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Operations;

public class SalePosTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SalePosTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.32.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterStoreAsync()
    {
        var client = CreateClientWithIp();
        var email = $"pos_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest("POS Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Cash_Credit_Mixed_Sales_And_Returns_WorkCorrectly()
    {
        var (client, _) = await RegisterStoreAsync();

        // 1. Setup Taxonomy & Product with Opening Stock
        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var catId = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!.Id;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unitId = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!.Id;

        var prodResp = await client.PostAsJsonAsync("/api/products", new CreateProductRequest($"Prod_{Guid.NewGuid():N}", catId, unitId, 25m, $"B_{Guid.NewGuid():N}"[..12]));
        var prodId = (await prodResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!.Id;

        // Opening Stock: 100 units at cost 10.00
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prodId, 100m, 10m));

        // 2. Setup Customer
        var cusResp = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest($"Cus_{Guid.NewGuid():N}", "+201200001111", null, 5000m));
        var cusId = (await cusResp.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data!.Id;

        // 3. Cash Sale (Walk-in, no customer required)
        var cashReq = new CreateSaleRequest(
            null,
            "Cash",
            0m,
            "Walk-in cash sale",
            new List<SaleLineItemRequest> { new(prodId, 10m, 25m) });

        var cashResp = await client.PostAsJsonAsync("/api/sales", cashReq);
        cashResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var cashSale = (await cashResp.Content.ReadFromJsonAsync<ApiResponse<SaleResponse>>())!.Data!;
        cashSale.TotalAmount.Should().Be(250m);
        cashSale.TotalCost.Should().Be(100m); // 10 * 10
        cashSale.CashAmount.Should().Be(250m);
        cashSale.CreditAmount.Should().Be(0m);

        // Verify stock decremented to 90
        var prodAfterCash = (await (await client.GetAsync($"/api/products/{prodId}")).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        prodAfterCash.CurrentStock.Should().Be(90m);

        // 4. Mixed Sale (Part Cash, Part Credit)
        var mixedReq = new CreateSaleRequest(
            cusId,
            "Mixed",
            50m, // Cash part
            "Mixed payment sale",
            new List<SaleLineItemRequest> { new(prodId, 10m, 25m) }); // Total 250m -> Credit = 200m

        var mixedResp = await client.PostAsJsonAsync("/api/sales", mixedReq);
        mixedResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var mixedSale = (await mixedResp.Content.ReadFromJsonAsync<ApiResponse<SaleResponse>>())!.Data!;
        mixedSale.CashAmount.Should().Be(50m);
        mixedSale.CreditAmount.Should().Be(200m);

        // Verify customer balance increased by 200m
        var cusAfterMixed = (await (await client.GetAsync($"/api/customers/{cusId}")).Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data!;
        cusAfterMixed.CurrentBalance.Should().Be(200m);

        // 5. Sale Return (Cash refund of 2 units from the cash sale)
        var returnReq = new CreateSaleReturnRequest(
            "Customer returned item",
            "Cash",
            new List<SaleReturnItemRequest> { new(prodId, 2m) });

        var returnResp = await client.PostAsJsonAsync($"/api/sales/{cashSale.Id}/returns", returnReq);
        returnResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var returnData = (await returnResp.Content.ReadFromJsonAsync<ApiResponse<SaleReturnResponse>>())!.Data!;
        returnData.TotalAmount.Should().Be(50m); // 2 * 25
        returnData.TotalCost.Should().Be(20m); // 2 * 10 (restocked at original cost)

        // Verify stock increased back by 2 units (90 - 10 + 2 = 82)
        var prodAfterReturn = (await (await client.GetAsync($"/api/products/{prodId}")).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        prodAfterReturn.CurrentStock.Should().Be(82m);
    }

    [Fact]
    public async Task Sale_Idempotency_Key_Prevents_Duplicate()
    {
        var (client, _) = await RegisterStoreAsync();

        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var catId = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!.Id;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unitId = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!.Id;

        var prodResp = await client.PostAsJsonAsync("/api/products", new CreateProductRequest($"Prod_{Guid.NewGuid():N}", catId, unitId, 30m));
        var prodId = (await prodResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!.Id;

        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prodId, 50m, 15m));

        var idempKey = Guid.NewGuid().ToString("N");
        var saleReq = new CreateSaleRequest(
            null,
            "Cash",
            0m,
            "Idemp sale",
            new List<SaleLineItemRequest> { new(prodId, 5m, 30m) });

        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/sales")
        {
            Content = JsonContent.Create(saleReq)
        };
        req1.Headers.Add("Idempotency-Key", idempKey);

        var resp1 = await client.SendAsync(req1);
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);
        var body1 = (await resp1.Content.ReadFromJsonAsync<ApiResponse<SaleResponse>>())!.Data!;

        // Send second request with exact same Idempotency-Key
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/sales")
        {
            Content = JsonContent.Create(saleReq)
        };
        req2.Headers.Add("Idempotency-Key", idempKey);

        var resp2 = await client.SendAsync(req2);
        resp2.StatusCode.Should().Be(HttpStatusCode.Created);
        var body2 = (await resp2.Content.ReadFromJsonAsync<ApiResponse<SaleResponse>>())!.Data!;

        body2.Id.Should().Be(body1.Id);
        body2.InvoiceNumber.Should().Be(body1.InvoiceNumber);

        // Verify stock only decremented once (50 - 5 = 45)
        var prodAfter = (await (await client.GetAsync($"/api/products/{prodId}")).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        prodAfter.CurrentStock.Should().Be(45m);
    }
}

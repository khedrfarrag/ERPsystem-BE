using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Inventory.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Sales.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Operations;

public class ConcurrencyAndStockTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ConcurrencyAndStockTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.33.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterStoreAsync()
    {
        var client = CreateClientWithIp();
        var email = $"race_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest("Race Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Insufficient_Stock_Rejects_Entire_Sale_With_Line_Breakdown()
    {
        var (client, _) = await RegisterStoreAsync();

        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var catId = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!.Id;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unitId = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!.Id;

        var prodResp = await client.PostAsJsonAsync("/api/products", new CreateProductRequest("Limited Product", catId, unitId, 50m));
        var prodId = (await prodResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!.Id;

        // Opening Stock: only 5 units available
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prodId, 5m, 20m));

        // Attempt to buy 10 units
        var saleReq = new CreateSaleRequest(
            null,
            "Cash",
            0m,
            "Over-stock attempt",
            new List<SaleLineItemRequest> { new(prodId, 10m, 50m) });

        var saleResp = await client.PostAsJsonAsync("/api/sales", saleReq);
        saleResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var err = await saleResp.Content.ReadFromJsonAsync<ApiResponse<object>>();
        err!.Code.Should().Be("INSUFFICIENT_STOCK");
        err.Errors.Should().Contain(e => e.Contains("Limited Product") && e.Contains("requested: 10") && e.Contains("available: 5"));

        // Verify stock remains untouched at 5
        var prodCheck = (await (await client.GetAsync($"/api/products/{prodId}")).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        prodCheck.CurrentStock.Should().Be(5m);
    }

    [Fact]
    public async Task Concurrent_Sales_Prevent_Oversell_Via_Pessimistic_Locking()
    {
        var (client, auth) = await RegisterStoreAsync();

        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var catId = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!.Id;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unitId = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!.Id;

        var prodResp = await client.PostAsJsonAsync("/api/products", new CreateProductRequest("Contested Product", catId, unitId, 100m));
        var prodId = (await prodResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!.Id;

        // Only 5 units in stock
        await client.PostAsJsonAsync("/api/inventory/opening-stock", new RecordOpeningStockRequest(prodId, 5m, 40m));

        // Create two separate client instances with separate connections
        var client1 = CreateClientWithIp();
        client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var client2 = CreateClientWithIp();
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var saleReq1 = new CreateSaleRequest(null, "Cash", 0m, "Contender 1", new List<SaleLineItemRequest> { new(prodId, 5m, 100m) });
        var saleReq2 = new CreateSaleRequest(null, "Cash", 0m, "Contender 2", new List<SaleLineItemRequest> { new(prodId, 5m, 100m) });

        // Dispatch concurrently
        var task1 = client1.PostAsJsonAsync("/api/sales", saleReq1);
        var task2 = client2.PostAsJsonAsync("/api/sales", saleReq2);

        var responses = await Task.WhenAll(task1, task2);

        // Exactly one should succeed (201 Created) and one should fail (400 Bad Request)
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var failureCount = responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest);

        successCount.Should().Be(1, "Exactly one cashier should secure the contested stock");
        failureCount.Should().Be(1, "The losing cashier should receive insufficient stock error");

        // Verify stock is exactly 0 (not negative!)
        var finalProd = (await (await client.GetAsync($"/api/products/{prodId}")).Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        finalProd.CurrentStock.Should().Be(0m);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Purchases.DTOs;
using RetailOS.Application.Suppliers.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Operations;

public class PurchaseOrderTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public PurchaseOrderTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.31.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterStoreAsync()
    {
        var client = CreateClientWithIp();
        var email = $"po_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest("PO Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Purchase_Workflow_Draft_Confirm_WAC_Return_Works()
    {
        var (client, _) = await RegisterStoreAsync();

        // 1. Setup Taxonomy & Product
        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var catId = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!.Id;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unitId = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!.Id;

        var prodResp = await client.PostAsJsonAsync("/api/products", new CreateProductRequest($"Prod_{Guid.NewGuid():N}", catId, unitId, 25m, $"B_{Guid.NewGuid():N}"[..12]));
        var prodId = (await prodResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!.Id;

        // 2. Setup Supplier
        var supResp = await client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest($"Sup_{Guid.NewGuid():N}", "+201012345678"));
        var supId = (await supResp.Content.ReadFromJsonAsync<ApiResponse<SupplierResponse>>())!.Data!.Id;

        // 3. Create Draft Purchase
        var draftReq = new CreatePurchaseRequest(
            supId,
            "INV-001",
            DateTimeOffset.UtcNow,
            "Initial order",
            new List<PurchaseLineItemRequest> { new(prodId, 50m, 10m) });

        var draftResp = await client.PostAsJsonAsync("/api/purchases", draftReq);
        draftResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var po = (await draftResp.Content.ReadFromJsonAsync<ApiResponse<PurchaseResponse>>())!.Data!;
        po.Status.Should().Be("Draft");
        po.TotalAmount.Should().Be(500m);

        // 4. Update Draft (Mutable edit per Q5)
        var updateReq = new UpdatePurchaseRequest(
            "INV-001-REV",
            DateTimeOffset.UtcNow,
            "Revised order",
            new List<PurchaseLineItemRequest> { new(prodId, 60m, 12m) });

        var updateResp = await client.PutAsJsonAsync($"/api/purchases/{po.Id}", updateReq);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPo = (await updateResp.Content.ReadFromJsonAsync<ApiResponse<PurchaseResponse>>())!.Data!;
        updatedPo.TotalAmount.Should().Be(720m);
        updatedPo.Items[0].Quantity.Should().Be(60m);

        // 5. Confirm Purchase
        var confirmResp = await client.PostAsync($"/api/purchases/{po.Id}/confirm", null);
        confirmResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmedPo = (await confirmResp.Content.ReadFromJsonAsync<ApiResponse<PurchaseResponse>>())!.Data!;
        confirmedPo.Status.Should().Be("Confirmed");

        // Verify product now has stock and purchaseCost is updated
        var getProdResp = await client.GetAsync($"/api/products/{prodId}");
        var prodData = (await getProdResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        prodData.CurrentStock.Should().Be(60m);
        prodData.PurchaseCost.Should().Be(12m);

        // Verify Supplier balance reflects payable
        var getSupResp = await client.GetAsync($"/api/suppliers/{supId}");
        var supData = (await getSupResp.Content.ReadFromJsonAsync<ApiResponse<SupplierResponse>>())!.Data!;
        supData.CurrentBalance.Should().Be(720m);

        // 6. Return 10 units of the purchase
        var returnReq = new CreatePurchaseReturnRequest(
            "Defective batch",
            new List<PurchaseReturnItemRequest> { new(prodId, 10m) });

        var returnResp = await client.PostAsJsonAsync($"/api/purchases/{po.Id}/returns", returnReq);
        returnResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var returnData = (await returnResp.Content.ReadFromJsonAsync<ApiResponse<PurchaseReturnResponse>>())!.Data!;
        returnData.TotalAmount.Should().Be(120m); // 10 * 12

        // Verify stock decremented by 10
        var getProdAfterReturn = await client.GetAsync($"/api/products/{prodId}");
        var prodAfterReturn = (await getProdAfterReturn.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        prodAfterReturn.CurrentStock.Should().Be(50m);

        // Verify supplier balance decremented
        var getSupAfterReturn = await client.GetAsync($"/api/suppliers/{supId}");
        var supAfterReturn = (await getSupAfterReturn.Content.ReadFromJsonAsync<ApiResponse<SupplierResponse>>())!.Data!;
        supAfterReturn.CurrentBalance.Should().Be(600m);
    }

    [Fact]
    public async Task Purchase_Idempotency_Key_Prevents_Duplicate()
    {
        var (client, _) = await RegisterStoreAsync();

        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var catId = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!.Id;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unitId = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!.Id;

        var prodResp = await client.PostAsJsonAsync("/api/products", new CreateProductRequest($"Prod_{Guid.NewGuid():N}", catId, unitId, 25m));
        var prodId = (await prodResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!.Id;

        var supResp = await client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest($"Sup_{Guid.NewGuid():N}", "+201012345678"));
        var supId = (await supResp.Content.ReadFromJsonAsync<ApiResponse<SupplierResponse>>())!.Data!.Id;

        var idempKey = Guid.NewGuid().ToString("N");
        var draftReq = new CreatePurchaseRequest(
            supId,
            "INV-IDEMP",
            DateTimeOffset.UtcNow,
            "Idemp order",
            new List<PurchaseLineItemRequest> { new(prodId, 10m, 15m) });

        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/purchases")
        {
            Content = JsonContent.Create(draftReq)
        };
        req1.Headers.Add("Idempotency-Key", idempKey);

        var resp1 = await client.SendAsync(req1);
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);
        var body1 = (await resp1.Content.ReadFromJsonAsync<ApiResponse<PurchaseResponse>>())!.Data!;

        // Send second request with exact same Idempotency-Key
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/purchases")
        {
            Content = JsonContent.Create(draftReq)
        };
        req2.Headers.Add("Idempotency-Key", idempKey);

        var resp2 = await client.SendAsync(req2);
        resp2.StatusCode.Should().Be(HttpStatusCode.Created);
        var body2 = (await resp2.Content.ReadFromJsonAsync<ApiResponse<PurchaseResponse>>())!.Data!;

        body2.Id.Should().Be(body1.Id);
        body2.PurchaseNumber.Should().Be(body1.PurchaseNumber);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Customers.DTOs;
using RetailOS.Application.Payments.DTOs;
using RetailOS.Application.Suppliers.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Operations;

public class PaymentSettlementTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public PaymentSettlementTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.34.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterStoreAsync()
    {
        var client = CreateClientWithIp();
        var email = $"pay_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest("Pay Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Customer_And_Supplier_Payment_Settlements_Work()
    {
        var (client, _) = await RegisterStoreAsync();

        // 1. Customer with 500 opening balance
        var cusResp = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest($"Cus_{Guid.NewGuid():N}", "+201011110000", null, 2000m, null, 500m));
        var cusId = (await cusResp.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data!.Id;

        // Customer pays 200
        var cusPayReq = new CreatePaymentRequest("Customer", cusId, null, 200m, "Cash", "REC-01", "Settlement");
        var cusPayResp = await client.PostAsJsonAsync("/api/payments", cusPayReq);
        cusPayResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify Customer balance decreased to 300
        var cusAfter = (await (await client.GetAsync($"/api/customers/{cusId}")).Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data!;
        cusAfter.CurrentBalance.Should().Be(300m);

        // 2. Supplier with 1000 opening balance
        var supResp = await client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest($"Sup_{Guid.NewGuid():N}", "+201022220000", null, null, 1000m));
        var supId = (await supResp.Content.ReadFromJsonAsync<ApiResponse<SupplierResponse>>())!.Data!.Id;

        // Pay supplier 600
        var supPayReq = new CreatePaymentRequest("Supplier", null, supId, 600m, "BankTransfer", "TR-890", "Invoice pay");
        var supPayResp = await client.PostAsJsonAsync("/api/payments", supPayReq);
        supPayResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify Supplier balance decreased to 400
        var supAfter = (await (await client.GetAsync($"/api/suppliers/{supId}")).Content.ReadFromJsonAsync<ApiResponse<SupplierResponse>>())!.Data!;
        supAfter.CurrentBalance.Should().Be(400m);
    }

    [Fact]
    public async Task Payment_Idempotency_Key_Prevents_Duplicate_Deduction()
    {
        var (client, _) = await RegisterStoreAsync();

        var cusResp = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest($"Cus_{Guid.NewGuid():N}", "+201033330000", null, null, null, 800m));
        var cusId = (await cusResp.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data!.Id;

        var idempKey = Guid.NewGuid().ToString("N");
        var payReq = new CreatePaymentRequest("Customer", cusId, null, 300m, "Cash", "REC-IDEMP", "Idempotent payment");

        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(payReq)
        };
        req1.Headers.Add("Idempotency-Key", idempKey);

        var resp1 = await client.SendAsync(req1);
        resp1.StatusCode.Should().Be(HttpStatusCode.Created);
        var body1 = (await resp1.Content.ReadFromJsonAsync<ApiResponse<PaymentResponse>>())!.Data!;

        // Send duplicate request
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/payments")
        {
            Content = JsonContent.Create(payReq)
        };
        req2.Headers.Add("Idempotency-Key", idempKey);

        var resp2 = await client.SendAsync(req2);
        resp2.StatusCode.Should().Be(HttpStatusCode.Created);
        var body2 = (await resp2.Content.ReadFromJsonAsync<ApiResponse<PaymentResponse>>())!.Data!;

        body2.Id.Should().Be(body1.Id);

        // Verify balance was only deducted once: 800 - 300 = 500
        var cusAfter = (await (await client.GetAsync($"/api/customers/{cusId}")).Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data!;
        cusAfter.CurrentBalance.Should().Be(500m);
    }
}

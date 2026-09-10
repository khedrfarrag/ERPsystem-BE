using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Customers.DTOs;
using RetailOS.Application.Suppliers.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Operations;

public class SupplierAndCustomerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SupplierAndCustomerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.30.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterStoreAsync()
    {
        var client = CreateClientWithIp();
        var email = $"ops_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest("Ops Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Supplier_Lifecycle_WorksCorrectly()
    {
        var (client, _) = await RegisterStoreAsync();

        // 1. Create Supplier with Opening Balance
        var supReq = new CreateSupplierRequest($"Supplier_{Guid.NewGuid():N}", "+201001112222", "Industrial Area", "Reliable", 1500m);
        var supResp = await client.PostAsJsonAsync("/api/suppliers", supReq);
        supResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var supData = (await supResp.Content.ReadFromJsonAsync<ApiResponse<SupplierResponse>>())!.Data!;
        supData.Name.Should().Be(supReq.Name);
        supData.CurrentBalance.Should().Be(1500m);

        // 2. Add Representative
        var repReq = new CreateRepresentativeRequest("Hany Rep", "+201112223333", "Sales Agent");
        var repResp = await client.PostAsJsonAsync($"/api/suppliers/{supData.Id}/representatives", repReq);
        repResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var repData = (await repResp.Content.ReadFromJsonAsync<ApiResponse<RepresentativeResponse>>())!.Data!;
        repData.Name.Should().Be("Hany Rep");

        // 3. Verify Supplier details include Rep
        var getResp = await client.GetAsync($"/api/suppliers/{supData.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var getData = (await getResp.Content.ReadFromJsonAsync<ApiResponse<SupplierResponse>>())!.Data!;
        getData.Representatives.Should().ContainSingle(r => r.Name == "Hany Rep");

        // 4. Verify Account Statement
        var stmtResp = await client.GetAsync($"/api/suppliers/{supData.Id}/statement");
        stmtResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var stmtData = (await stmtResp.Content.ReadFromJsonAsync<ApiResponse<AccountStatementResponse>>())!.Data!;
        stmtData.CurrentBalance.Should().Be(1500m);
        stmtData.Transactions.Should().ContainSingle(t => t.Type == "OpeningBalance" && t.Amount == 1500m);

        // 5. Duplicate Name -> Conflict
        var dupResp = await client.PostAsJsonAsync("/api/suppliers", supReq);
        dupResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Customer_Lifecycle_WorksCorrectly()
    {
        var (client, _) = await RegisterStoreAsync();

        // 1. Create Customer
        var cusReq = new CreateCustomerRequest($"Customer_{Guid.NewGuid():N}", "+201223334444", "Cairo", 5000m, "Regular", 250m);
        var cusResp = await client.PostAsJsonAsync("/api/customers", cusReq);
        cusResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var cusData = (await cusResp.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data!;
        cusData.Name.Should().Be(cusReq.Name);
        cusData.CreditLimit.Should().Be(5000m);
        cusData.CurrentBalance.Should().Be(250m);

        // 2. Update Customer
        var updateReq = new UpdateCustomerRequest(cusData.Name, "+201223339999", "New Cairo", 7000m, "VIP");
        var updateResp = await client.PutAsJsonAsync($"/api/customers/{cusData.Id}", updateReq);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateData = (await updateResp.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data!;
        updateData.Phone.Should().Be("+201223339999");
        updateData.CreditLimit.Should().Be(7000m);

        // 3. Statement
        var stmtResp = await client.GetAsync($"/api/customers/{cusData.Id}/statement");
        stmtResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var stmtData = (await stmtResp.Content.ReadFromJsonAsync<ApiResponse<AccountStatementResponse>>())!.Data!;
        stmtData.CurrentBalance.Should().Be(250m);
        stmtData.Transactions.Should().ContainSingle(t => t.Type == "OpeningBalance");

        // 4. Duplicate Name -> Conflict
        var dupResp = await client.PostAsJsonAsync("/api/customers", cusReq);
        dupResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}

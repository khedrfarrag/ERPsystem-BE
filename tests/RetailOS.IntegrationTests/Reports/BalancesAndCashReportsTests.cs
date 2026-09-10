using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.CashRegister.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Customers.DTOs;
using RetailOS.Application.Expenses.DTOs;
using RetailOS.Application.Inventory.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Reports.DTOs;
using RetailOS.Application.Sales.DTOs;
using RetailOS.Application.Suppliers.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Reports;

public class BalancesAndCashReportsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public BalancesAndCashReportsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.43.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterStoreAsync()
    {
        var client = CreateClientWithIp();
        var email = $"rep_bc_{Guid.NewGuid():N}@test.com";
        var reg = new RegisterStoreRequest("Balances & Cash Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Customer_Supplier_Balances_And_Cash_Audit_Reports_Work()
    {
        var (client, _) = await RegisterStoreAsync();

        // 1. Setup Customer with 500 debt and Supplier with 1200 payable
        var cus = (await (await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest($"Cus_{Guid.NewGuid():N}", "+201011112222", null, 2000m, null, 500m))).Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data!;
        var sup = (await (await client.PostAsJsonAsync("/api/suppliers", new CreateSupplierRequest($"Sup_{Guid.NewGuid():N}", "+201022223333", null, null, 1200m))).Content.ReadFromJsonAsync<ApiResponse<SupplierResponse>>())!.Data!;

        // Check Customer Balances Report
        var cusBalResp = await client.GetAsync("/api/reports/balances/customers");
        cusBalResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var cusBal = (await cusBalResp.Content.ReadFromJsonAsync<ApiResponse<CustomerBalancesReportResponse>>())!.Data!;
        cusBal.TotalReceivables.Should().Be(500m);
        cusBal.Debtors.Should().ContainSingle(d => d.CustomerId == cus.Id);

        // Check Supplier Balances Report
        var supBalResp = await client.GetAsync("/api/reports/balances/suppliers");
        supBalResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var supBal = (await supBalResp.Content.ReadFromJsonAsync<ApiResponse<SupplierBalancesReportResponse>>())!.Data!;
        supBal.TotalPayables.Should().Be(1200m);
        supBal.Creditors.Should().ContainSingle(c => c.SupplierId == sup.Id);

        // 2. Cash Register Operations:
        // Open float: 1000
        await client.PostAsJsonAsync("/api/cash-register/open", new OpenFloatRequest(1000m));

        // Create Expense: 100 Cash
        var expCat = (await (await client.PostAsJsonAsync("/api/expenses/categories", new CreateExpenseCategoryRequest($"ExpCat_{Guid.NewGuid():N}"))).Content.ReadFromJsonAsync<ApiResponse<ExpenseCategoryResponse>>())!.Data!;
        await client.PostAsJsonAsync("/api/expenses", new CreateExpenseRequest(expCat.Id, 100m, DateTimeOffset.UtcNow, "Cash"));

        // Close register with counted amount 890 (Discrepancy: -10)
        await client.PostAsJsonAsync("/api/cash-register/close", new CloseRegisterRequest(890m));

        // 3. Query Cash Register Audit Report
        var cashAuditResp = await client.GetAsync("/api/reports/cash-register");
        cashAuditResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var cashAudit = (await cashAuditResp.Content.ReadFromJsonAsync<ApiResponse<CashRegisterAuditReportResponse>>())!.Data!;

        cashAudit.TotalOpeningFloats.Should().Be(1000m);
        cashAudit.TotalExpenseOutflows.Should().Be(100m);
        cashAudit.TotalDiscrepancies.Should().Be(-10m);
        cashAudit.DailySummaries.Should().NotBeEmpty();

        // CSV Export of Cash Audit
        var csvResp = await client.GetAsync("/api/reports/cash-register?format=csv");
        csvResp.StatusCode.Should().Be(HttpStatusCode.OK);
        csvResp.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }
}

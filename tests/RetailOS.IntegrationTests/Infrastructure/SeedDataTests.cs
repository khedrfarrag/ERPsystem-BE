using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Common.Interfaces;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Infrastructure;

public class SeedDataTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public SeedDataTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seed_Demo_Store_Successfully_Creates_Rich_Catalog_And_Users()
    {
        var client = _factory.CreateClient();
        
        var resp = await client.PostAsync("/api/seed/demo-store?force=true", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = (await resp.Content.ReadFromJsonAsync<ApiResponse<DemoSeedResult>>())!.Data!;
        result.Success.Should().BeTrue();
        result.ProductsCreated.Should().Be(15);
        result.SalesCreated.Should().BeGreaterThan(0);
        result.StoreName.Should().Be("مؤسسة الأمل للمنظفات والكيماويات");
        result.OwnerEmail.Should().Be("owner@retailos.com");
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Catalog;

public class ProductCrudTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ProductCrudTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.13.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterOwnerAsync()
    {
        var client = CreateClientWithIp();
        var email = $"prod_owner_{Guid.NewGuid():N}@t.com";
        var reg = new RegisterStoreRequest("Prod Test Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Product_Lifecycle_WorksCorrectly()
    {
        var (client, _) = await RegisterOwnerAsync();

        // 1. Create Category and Unit
        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat_{Guid.NewGuid():N}", null));
        var cat = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;

        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"Unit_{Guid.NewGuid():N}", $"u_{Guid.NewGuid():N}"[..6], null));
        var unit = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;

        // 2. Create Product
        var prodName = $"Product_{Guid.NewGuid():N}";
        var barcode = $"BAR_{Guid.NewGuid():N}"[..12];
        var createReq = new CreateProductRequest(prodName, cat.Id, unit.Id, 25.50m, barcode, "Test description", 18.00m, 10m, null);
        var createResp = await client.PostAsJsonAsync("/api/products", createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdProd = (await createResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        createdProd.Name.Should().Be(prodName);
        createdProd.SellingPrice.Should().Be(25.50m);
        createdProd.Category.Id.Should().Be(cat.Id);
        createdProd.Unit.Id.Should().Be(unit.Id);
        createdProd.CurrentStock.Should().Be(0m);

        // 3. Duplicate name -> 409
        var dupNameReq = new CreateProductRequest(prodName.ToUpperInvariant(), cat.Id, unit.Id, 30m, null);
        var dupNameResp = await client.PostAsJsonAsync("/api/products", dupNameReq);
        dupNameResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 4. Duplicate barcode -> 409
        var dupBarcodeReq = new CreateProductRequest($"Other_{prodName}", cat.Id, unit.Id, 30m, barcode);
        var dupBarcodeResp = await client.PostAsJsonAsync("/api/products", dupBarcodeReq);
        dupBarcodeResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 5. Barcode lookup
        var barcodeResp = await client.GetAsync($"/api/products/barcode/{barcode}");
        barcodeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var barcodeProd = (await barcodeResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        barcodeProd.Id.Should().Be(createdProd.Id);

        // 6. Update product
        var updatedName = $"Updated_{prodName}";
        var updateReq = new UpdateProductRequest(updatedName, cat.Id, unit.Id, 28.00m, barcode, "Updated desc", 20.00m, 15m, null);
        var updateResp = await client.PutAsJsonAsync($"/api/products/{createdProd.Id}", updateReq);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProd = (await updateResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        updatedProd.Name.Should().Be(updatedName);
        updatedProd.SellingPrice.Should().Be(28.00m);

        // 7. Deactivate product
        var deactResp = await client.PatchAsJsonAsync($"/api/products/{createdProd.Id}/status", new UpdateProductStatusRequest(false));
        deactResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactProd = (await deactResp.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data!;
        deactProd.IsActive.Should().BeFalse();

        // 8. Delete product
        var delResp = await client.DeleteAsync($"/api/products/{createdProd.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 9. Verify gone
        var getResp = await client.GetAsync($"/api/products/{createdProd.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

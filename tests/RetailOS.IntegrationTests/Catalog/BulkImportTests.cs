using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.Application.Products.DTOs;
using RetailOS.Application.Units.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Catalog;

public class BulkImportTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public BulkImportTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.15.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth, CategoryResponse Cat, UnitResponse Unit)> SetupOwnerWithTaxonomyAsync()
    {
        var client = CreateClientWithIp();
        var email = $"import_owner_{Guid.NewGuid():N}@t.com";
        var reg = new RegisterStoreRequest("Import Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var catName = $"ImportCat_{Guid.NewGuid():N}"[..15];
        var catResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(catName, null));
        var cat = (await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;

        var unitSymbol = $"u_{Guid.NewGuid():N}"[..6];
        var unitResp = await client.PostAsJsonAsync("/api/units", new CreateUnitRequest($"ImportUnit_{unitSymbol}", unitSymbol, null));
        var unit = (await unitResp.Content.ReadFromJsonAsync<ApiResponse<UnitResponse>>())!.Data!;

        return (client, auth, cat, unit);
    }

    [Fact]
    public async Task BulkImport_CsvPreviewAndCommit_WorksCorrectly()
    {
        var (client, _, cat, unit) = await SetupOwnerWithTaxonomyAsync();

        var p1Name = $"CsvProd1_{Guid.NewGuid():N}";
        var p2Name = $"CsvProd2_{Guid.NewGuid():N}";
        var p3Name = $"CsvProd3_InvalidCat_{Guid.NewGuid():N}";

        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("name,barcode,category_name,unit_symbol,selling_price,purchase_cost,min_stock_level,description");
        csvBuilder.AppendLine($"{p1Name},BC1_{Guid.NewGuid():N}[..8],{cat.Name},{unit.Symbol},15.00,10.00,5,First prod");
        csvBuilder.AppendLine($"{p2Name},BC2_{Guid.NewGuid():N}[..8],{cat.Name},{unit.Symbol},25.50,20.00,10,Second prod");
        csvBuilder.AppendLine($"{p3Name},,NonExistentCategory,{unit.Symbol},30.00,25.00,2,Invalid row");

        var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());

        // 1. Preview
        using var previewContent = new MultipartFormDataContent();
        previewContent.Add(new ByteArrayContent(csvBytes), "file", "products.csv");

        var previewResp = await client.PostAsync("/api/products/import/preview", previewContent);
        previewResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = (await previewResp.Content.ReadFromJsonAsync<ApiResponse<ImportPreviewResponse>>())!.Data!;

        preview.TotalRows.Should().Be(3);
        preview.ValidRows.Should().Be(2);
        preview.ErrorRows.Should().Be(1);
        preview.Errors.Should().Contain(e => e.Field == "category_name");

        // 2. Commit
        using var commitContent = new MultipartFormDataContent();
        commitContent.Add(new ByteArrayContent(csvBytes), "file", "products.csv");

        var commitResp = await client.PostAsync("/api/products/import/commit", commitContent);
        commitResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var commitResult = (await commitResp.Content.ReadFromJsonAsync<ApiResponse<ImportCommitResponse>>())!.Data!;

        commitResult.TotalProcessed.Should().Be(3);
        commitResult.Created.Should().Be(2);
        commitResult.SkippedInvalid.Should().Be(1);
        commitResult.SkippedDuplicate.Should().Be(0);

        // 3. Re-commit same file (both valid rows should now be detected as duplicates)
        using var recommitContent = new MultipartFormDataContent();
        recommitContent.Add(new ByteArrayContent(csvBytes), "file", "products.csv");

        var recommitResp = await client.PostAsync("/api/products/import/commit", recommitContent);
        recommitResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var recommitResult = (await recommitResp.Content.ReadFromJsonAsync<ApiResponse<ImportCommitResponse>>())!.Data!;

        recommitResult.Created.Should().Be(0);
        recommitResult.SkippedDuplicate.Should().Be(2);
        recommitResult.SkippedInvalid.Should().Be(1);
    }

    [Fact]
    public async Task BulkImport_XlsxPreview_WorksCorrectly()
    {
        var (client, _, cat, unit) = await SetupOwnerWithTaxonomyAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Products");

        // Header
        worksheet.Cell(1, 1).Value = "name";
        worksheet.Cell(1, 2).Value = "barcode";
        worksheet.Cell(1, 3).Value = "category_name";
        worksheet.Cell(1, 4).Value = "unit_symbol";
        worksheet.Cell(1, 5).Value = "selling_price";
        worksheet.Cell(1, 6).Value = "purchase_cost";
        worksheet.Cell(1, 7).Value = "min_stock_level";
        worksheet.Cell(1, 8).Value = "description";

        // Row 2: valid
        var xName = $"XlsxProd_{Guid.NewGuid():N}";
        worksheet.Cell(2, 1).Value = xName;
        worksheet.Cell(2, 2).Value = $"XB_{Guid.NewGuid():N}"[..8];
        worksheet.Cell(2, 3).Value = cat.Name;
        worksheet.Cell(2, 4).Value = unit.Symbol;
        worksheet.Cell(2, 5).Value = "99.99";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var xlsxBytes = ms.ToArray();

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(xlsxBytes), "file", "products.xlsx");

        var resp = await client.PostAsync("/api/products/import/preview", content);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = (await resp.Content.ReadFromJsonAsync<ApiResponse<ImportPreviewResponse>>())!.Data!;
        preview.TotalRows.Should().Be(1);
        preview.ValidRows.Should().Be(1);
        preview.ErrorRows.Should().Be(0);
    }
}

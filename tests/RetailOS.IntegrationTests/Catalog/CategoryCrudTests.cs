using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.Application.Categories.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Catalog;

public class CategoryCrudTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CategoryCrudTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithIp()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.11.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<(HttpClient Client, AuthResponse Auth)> RegisterOwnerAsync()
    {
        var client = CreateClientWithIp();
        var email = $"cat_owner_{Guid.NewGuid():N}@t.com";
        var reg = new RegisterStoreRequest("Cat Test Store", "Retail", "Owner", "Boss", email, "Pass123456!");
        var regResp = await client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = (await regResp.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Category_Lifecycle_WorksCorrectly()
    {
        var (client, _) = await RegisterOwnerAsync();

        // 1. Create Category
        var catName = $"Beverages_{Guid.NewGuid():N}";
        var createResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(catName, "Drinks and beverages"));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdCat = (await createResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;
        createdCat.Name.Should().Be(catName);
        createdCat.IsActive.Should().BeTrue();

        // 2. Duplicate name (case-insensitive) -> 409 Conflict
        var dupResp = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(catName.ToUpperInvariant(), "Duplicate"));
        dupResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 3. List categories
        var listResp = await client.GetAsync("/api/categories");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var listData = (await listResp.Content.ReadFromJsonAsync<ApiResponse<CategoryListResponse>>())!.Data!;
        listData.Items.Should().Contain(c => c.Id == createdCat.Id);

        // 4. Update Category
        var updatedName = $"Updated_{catName}";
        var updateResp = await client.PutAsJsonAsync($"/api/categories/{createdCat.Id}", new UpdateCategoryRequest(updatedName, "New desc"));
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCat = (await updateResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;
        updatedCat.Name.Should().Be(updatedName);

        // 5. Deactivate Category
        var deactResp = await client.PatchAsJsonAsync($"/api/categories/{createdCat.Id}/status", new UpdateCategoryStatusRequest(false));
        deactResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactCat = (await deactResp.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data!;
        deactCat.IsActive.Should().BeFalse();

        // 6. Soft Delete Category
        var delResp = await client.DeleteAsync($"/api/categories/{createdCat.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 7. Verify not found after delete
        var getResp = await client.GetAsync($"/api/categories/{createdCat.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

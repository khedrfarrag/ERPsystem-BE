using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using RetailOS.Shared;
using Xunit;

namespace RetailOS.IntegrationTests.Auth;

public class AuthRegistrationAndLoginTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthRegistrationAndLoginTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateTestClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.0.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    [Fact]
    public async Task Register_WithValidData_Returns201AndTokens()
    {
        // Arrange
        var client = CreateTestClient();
        var uniqueEmail = $"owner_{Guid.NewGuid():N}@test.com";
        var request = new RegisterStoreRequest(
            StoreName: "Al-Baraka Market",
            BusinessType: "Supermarket",
            OwnerFirstName: "Hassan",
            OwnerLastName: "Ibrahim",
            Email: uniqueEmail,
            Password: "SecurePassword123!"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        content.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        content.Data.User.Email.Should().Be(uniqueEmail.ToLowerInvariant());
        content.Data.User.Role.Should().Be("Owner");
        content.Data.User.StoreName.Should().Be("Al-Baraka Market");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409Conflict()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"dup_{Guid.NewGuid():N}@test.com";
        var request = new RegisterStoreRequest(
            StoreName: "Store 1",
            BusinessType: "Retail",
            OwnerFirstName: "Ali",
            OwnerLastName: "Ahmed",
            Email: email,
            Password: "SecurePassword123!"
        );

        var firstResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - register again with same email
        var secondResponse = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await secondResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Code.Should().Be("EMAIL_ALREADY_EXISTS");
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndTokens()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"login_{Guid.NewGuid():N}@test.com";
        var password = "SecurePassword123!";
        var registerRequest = new RegisterStoreRequest("Login Store", "Grocery", "Tarek", "Nour", email, password);
        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(email, password);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        content.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401Unauthorized()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"wrongpass_{Guid.NewGuid():N}@test.com";
        var registerRequest = new RegisterStoreRequest("Store", "Grocery", "Omar", "Ali", email, "CorrectPass123!");
        await client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest(email, "WrongPassword123!");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        content!.Code.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsCurrentUserProfileAndStore()
    {
        // Arrange
        var client = CreateTestClient();
        var email = $"me_{Guid.NewGuid():N}@test.com";
        var registerRequest = new RegisterStoreRequest("Me Store", "Detergents", "Mostafa", "Kamal", email, "Pass123456!");
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var authData = (await registerResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>())!.Data!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.AccessToken);

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<CurrentUserResponse>>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data!.Email.Should().Be(email.ToLowerInvariant());
        content.Data.FirstName.Should().Be("Mostafa");
        content.Data.Role.Should().Be("Owner");
        content.Data.Store.Name.Should().Be("Me Store");
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

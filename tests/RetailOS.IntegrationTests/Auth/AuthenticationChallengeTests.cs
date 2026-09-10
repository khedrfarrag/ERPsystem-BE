using System.Net;
using FluentAssertions;
using RetailOS.IntegrationTests.Infrastructure;
using Xunit;

namespace RetailOS.IntegrationTests.Auth;

public class AuthenticationChallengeTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthenticationChallengeTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateUnauthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.4.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    [Theory]
    [InlineData("/api/stores/current")]
    [InlineData("/api/auth/me")]
    public async Task ProtectedEndpoints_WhenUnauthenticated_Return401(string endpoint)
    {
        // Arrange
        var client = CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

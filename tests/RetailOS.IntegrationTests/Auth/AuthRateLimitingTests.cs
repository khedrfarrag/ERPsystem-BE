using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RetailOS.Application.Auth.DTOs;
using RetailOS.IntegrationTests.Infrastructure;
using Xunit;

namespace RetailOS.IntegrationTests.Auth;

public class AuthRateLimitingTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthRateLimitingTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WhenExceedingRateLimit_Returns429TooManyRequests()
    {
        // Arrange
        var dedicatedClient = _client;
        dedicatedClient.DefaultRequestHeaders.Add("X-Forwarded-For", "10.99.99.99");

        var dummyRequest = new LoginRequest("test_rate_limit@example.com", "WrongPassword123!");
        var responses = new List<HttpResponseMessage>();

        // Act - Send 7 requests rapidly (limit is 5 per window)
        for (int i = 0; i < 7; i++)
        {
            var response = await dedicatedClient.PostAsJsonAsync("/api/auth/login", dummyRequest);
            responses.Add(response);
        }

        // Assert - At least one response should be 429 Too Many Requests
        responses.Should().Contain(r => r.StatusCode == HttpStatusCode.TooManyRequests);
    }
}

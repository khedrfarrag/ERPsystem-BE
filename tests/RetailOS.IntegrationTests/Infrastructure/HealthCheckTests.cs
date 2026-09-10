using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace RetailOS.IntegrationTests.Infrastructure;

public class HealthCheckTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_Returns200Ok()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReady_WhenDatabaseConnected_Returns200Ok()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HealthReadyResponse>();
        content.Should().NotBeNull();
        content!.Status.Should().Be("Ready");
        content.Database.Should().Be("Connected");
    }

    private record HealthReadyResponse(string Status, string Database, DateTime Timestamp);
}

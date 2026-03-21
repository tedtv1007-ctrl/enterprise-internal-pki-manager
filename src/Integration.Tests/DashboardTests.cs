using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using EnterprisePKI.Shared.Models;
using Integration.Tests.Fixtures;

namespace Integration.Tests;

public class DashboardTests : IClassFixture<PkiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DashboardTests(PkiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStats_ReturnsSuccess()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("totalCertificates");
        content.Should().Contain("expiringSoon");
        content.Should().Contain("activeAgents");
        content.Should().Contain("pqcReadyCertificates");
    }

    [Fact]
    public async Task GetStats_ReturnsNonNegativeValues()
    {
        // Act
        var response = await _client.GetAsync("/api/dashboard/stats");
        var stats = await response.Content.ReadFromJsonAsync<DashboardStatsDto>();

        // Assert
        stats.Should().NotBeNull();
        stats!.TotalCertificates.Should().BeGreaterThanOrEqualTo(0);
        stats.ExpiringSoon.Should().BeGreaterThanOrEqualTo(0);
        stats.ActiveAgents.Should().BeGreaterThanOrEqualTo(0);
        stats.PqcReadyCertificates.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetAllAgents_ReturnsPaginatedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/agents?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedResult = await response.Content.ReadFromJsonAsync<PaginatedResult<dynamic>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().NotBeNull();
    }
}

public record DashboardStatsDto
{
    public int TotalCertificates { get; init; }
    public int ExpiringSoon { get; init; }
    public int ActiveAgents { get; init; }
    public int PqcReadyCertificates { get; init; }
}

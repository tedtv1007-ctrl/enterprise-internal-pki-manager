using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Portal.Tests;

public class PortalSecurityPipelineTests : IClassFixture<PortalSecurityPipelineTests.PortalFactory>
{
    private const string ValidPortalApiToken = "portal-api-test-token";
    private readonly PortalFactory _factory;

    public PortalSecurityPipelineTests(PortalFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CertificatesEndpoint_WithoutBearerToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/security/probe");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CertificatesEndpoint_WithValidBearerToken_IsNotUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ValidPortalApiToken);

        // Act
        var response = await client.GetAsync("/api/security/probe");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public sealed class PortalFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Portal:ApiAuthToken"] = ValidPortalApiToken,
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=portal_test;Username=test;Password=test"
                });
            });
        }
    }
}
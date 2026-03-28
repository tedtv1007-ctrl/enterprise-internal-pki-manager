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
        // Arrange — use HTTPS since HTTP redirects
        using var client = _factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        // Act
        var response = await client.GetAsync("/api/security/probe");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CertificatesEndpoint_WithValidBearerToken_IsNotUnauthorized()
    {
        // Arrange — use HTTPS since HTTP redirects
        using var client = _factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ValidPortalApiToken);

        // Act
        var response = await client.GetAsync("/api/security/probe");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WeatherForecast_ShouldNotExist()
    {
        // Arrange – scaffold endpoints are unauthenticated attack surface
        using var client = _factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        // Act
        var response = await client.GetAsync("/weatherforecast");

        // Assert – endpoint must be removed entirely
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HttpRequest_ShouldRedirectToHttps()
    {
        // Arrange – disable auto-redirect to observe the 307
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("http://localhost/api/security/probe");

        // Assert – HTTPS redirection middleware should redirect
        var statusCode = (int)response.StatusCode;
        statusCode.Should().BeOneOf(new[] { 301, 302, 307, 308 },
            "Portal should redirect HTTP to HTTPS");
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
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=portal_test;Username=test;Password=test",
                    ["HTTPS_PORT"] = "443"
                });
            });
        }
    }
}
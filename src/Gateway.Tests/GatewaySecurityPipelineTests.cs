using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EnterprisePKI.Gateway.Controllers;
using EnterprisePKI.Shared.Interfaces;
using EnterprisePKI.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gateway.Tests;

public class GatewaySecurityPipelineTests : IClassFixture<GatewaySecurityPipelineTests.GatewayFactory>
{
    private const string ValidGatewayToken = "gateway-test-token";
    private readonly GatewayFactory _factory;

    public GatewaySecurityPipelineTests(GatewayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Issue_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var payload = new CaController.IssueRequest
        {
            Csr = "test-csr",
            TemplateName = "WebServer"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/ca/issue", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Issue_WhenAuthenticatedWithValidServiceToken_ReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ValidGatewayToken);
        client.DefaultRequestHeaders.Add("X-Client-Id", "authorized-success");

        var payload = new CaController.IssueRequest
        {
            Csr = "test-csr",
            TemplateName = "WebServer"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/ca/issue", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Issue_WhenRateLimitExceeded_ReturnsTooManyRequests()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ValidGatewayToken);
        client.DefaultRequestHeaders.Add("X-Client-Id", "rate-limit-test");

        var payload = new CaController.IssueRequest
        {
            Csr = "test-csr",
            TemplateName = "WebServer"
        };

        // Act
        var first = await client.PostAsJsonAsync("/api/ca/issue", payload);
        var second = await client.PostAsJsonAsync("/api/ca/issue", payload);

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    public sealed class GatewayFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var existingRegistration = services.SingleOrDefault(s => s.ServiceType == typeof(ICertificateAuthority));
                if (existingRegistration is not null)
                {
                    services.Remove(existingRegistration);
                }

                services.AddSingleton<ICertificateAuthority, StubCertificateAuthority>();
            });

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Gateway:ServiceAuthToken"] = ValidGatewayToken,
                    ["Gateway:RateLimiting:PermitLimit"] = "1",
                    ["Gateway:RateLimiting:WindowSeconds"] = "60"
                });
            });
        }
    }

    private sealed class StubCertificateAuthority : ICertificateAuthority
    {
        public Task<Certificate> IssueCertificateAsync(string csrPem, string templateName)
        {
            return Task.FromResult(new Certificate
            {
                Id = Guid.NewGuid(),
                CommonName = "issued.test.internal",
                SerialNumber = "TEST-SERIAL",
                Thumbprint = "TEST-THUMBPRINT",
                IssuerDN = "CN=Gateway Test CA",
                NotBefore = DateTime.UtcNow,
                NotAfter = DateTime.UtcNow.AddYears(1),
                Algorithm = "RSA",
                KeySize = 2048,
                Status = "Active"
            });
        }

        public Task<bool> RevokeCertificateAsync(string serialNumber, int reason = 0)
        {
            return Task.FromResult(true);
        }
    }
}
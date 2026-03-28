using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using EnterprisePKI.Shared.Models;
using Integration.Tests.Fixtures;

namespace Integration.Tests;

public class DiscoveryReportTests : IClassFixture<PkiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DiscoveryReportTests(PkiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SubmitDiscovery_ProcessesSuccessfully()
    {
        // Arrange
        var report = new DiscoveryReport
        {
            Hostname = "discovery-test-host",
            Certificates = new List<CertificateDiscovery>
            {
                new CertificateDiscovery
                {
                    Thumbprint = Guid.NewGuid().ToString("N"),
                    CommonName = "discovered-cert.test.com",
                    NotAfter = DateTime.UtcNow.AddDays(180),
                    StoreLocation = "My/LocalMachine"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/certificates/discovery", report);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitDiscovery_MultipleCerts_AllProcessed()
    {
        // Arrange
        var report = new DiscoveryReport
        {
            Hostname = "multi-cert-host",
            Certificates = new List<CertificateDiscovery>
            {
                new CertificateDiscovery
                {
                    Thumbprint = Guid.NewGuid().ToString("N"),
                    CommonName = "cert1.test.com",
                    NotAfter = DateTime.UtcNow.AddDays(90)
                },
                new CertificateDiscovery
                {
                    Thumbprint = Guid.NewGuid().ToString("N"),
                    CommonName = "cert2.test.com",
                    NotAfter = DateTime.UtcNow.AddDays(60)
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/certificates/discovery", report);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitDiscovery_WithMissingHostname_ReturnsBadRequest()
    {
        // Arrange
        var report = new DiscoveryReport
        {
            Hostname = "",
            Certificates = new List<CertificateDiscovery>
            {
                new CertificateDiscovery
                {
                    Thumbprint = Guid.NewGuid().ToString("N"),
                    CommonName = "cert-invalid-host.test.com",
                    NotAfter = DateTime.UtcNow.AddDays(90)
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/certificates/discovery", report);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        error.Should().NotBeNull();
        error!.Error.Should().Be("ValidationError");
    }

    [Fact]
    public async Task SubmitDiscovery_WithNoCertificates_ReturnsBadRequest()
    {
        // Arrange
        var report = new DiscoveryReport
        {
            Hostname = "empty-certs-host",
            Certificates = new List<CertificateDiscovery>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/certificates/discovery", report);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        error.Should().NotBeNull();
        error!.Error.Should().Be("ValidationError");
    }
}

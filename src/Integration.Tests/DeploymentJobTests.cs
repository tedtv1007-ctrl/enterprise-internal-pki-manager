using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using EnterprisePKI.Shared.Models;
using Integration.Tests.Fixtures;

namespace Integration.Tests;

public class DeploymentJobTests : IClassFixture<PkiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DeploymentJobTests(PkiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllJobs_ReturnsSuccessAndPaginatedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/deployments/jobs?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedResult = await response.Content.ReadFromJsonAsync<PaginatedResult<DeploymentJob>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateJob_ThenUpdateStatus_FullLifecycle()
    {
        // First, create a certificate to reference
        var cert = new Certificate
        {
            CommonName = "deploy-test.example.com",
            SerialNumber = $"SN-DEPLOY-{Guid.NewGuid().ToString("N")[..8]}",
            Thumbprint = Guid.NewGuid().ToString("N"),
            IssuerDN = "CN=Deploy Test CA",
            NotBefore = DateTime.UtcNow,
            NotAfter = DateTime.UtcNow.AddYears(1),
            Algorithm = "RSA",
            KeySize = 2048,
            Status = "Active"
        };
        var certResponse = await _client.PostAsJsonAsync("/api/certificates", cert);
        var createdCert = await certResponse.Content.ReadFromJsonAsync<Certificate>();

        // Create deployment job
        var job = new DeploymentJob
        {
            CertificateId = createdCert!.Id,
            TargetHostname = "test-deploy-host",
            StoreLocation = "My/LocalMachine"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/deployments/create", job);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdJob = await createResponse.Content.ReadFromJsonAsync<DeploymentJob>();
        createdJob.Should().NotBeNull();
        createdJob!.Status.Should().Be("Pending");

        // Update job status
        var updateResponse = await _client.PostAsJsonAsync(
            $"/api/deployments/jobs/{createdJob.Id}/status",
            new { Status = "Completed", ErrorMessage = (string?)null });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPendingJobs_FiltersByHostname()
    {
        // Act
        var response = await _client.GetAsync("/api/deployments/jobs/nonexistent-host");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jobs = await response.Content.ReadFromJsonAsync<List<DeploymentJob>>();
        jobs.Should().NotBeNull();
        jobs.Should().BeEmpty();
    }
}

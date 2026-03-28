using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using EnterprisePKI.Shared.Models;
using Integration.Tests.Fixtures;

namespace Integration.Tests;

public class CertificateLifecycleTests : IClassFixture<PkiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CertificateLifecycleTests(PkiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsSuccessAndPaginatedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/certificates?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedResult = await response.Content.ReadFromJsonAsync<PaginatedResult<Certificate>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(10);
        pagedResult.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAndRetrieve_FullLifecycle()
    {
        // Arrange
        var cert = new Certificate
        {
            CommonName = "lifecycle-test.example.com",
            SerialNumber = $"SN-INTTEST-{Guid.NewGuid().ToString("N")[..8]}",
            Thumbprint = Guid.NewGuid().ToString("N"),
            IssuerDN = "CN=Integration Test CA",
            NotBefore = DateTime.UtcNow,
            NotAfter = DateTime.UtcNow.AddYears(1),
            Algorithm = "RSA",
            KeySize = 4096,
            IsPQC = false,
            Status = "Active"
        };

        // Act - Create
        var createResponse = await _client.PostAsJsonAsync("/api/certificates", cert);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Certificate>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();

        // Act - GetById
        var getResponse = await _client.GetAsync($"/api/certificates/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrieved = await getResponse.Content.ReadFromJsonAsync<Certificate>();
        retrieved.Should().NotBeNull();
        retrieved!.CommonName.Should().Be("lifecycle-test.example.com");
        retrieved.Algorithm.Should().Be("RSA");
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404_WithApiError()
    {
        // Act
        var response = await _client.GetAsync($"/api/certificates/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        error.Should().NotBeNull();
        error!.Error.Should().Be("NotFound");
    }

    [Fact]
    public async Task RequestCertificate_WhenGatewayIsUnavailable_ReturnsCreatedAndLocation_ForPendingRequest()
    {
        // Arrange
        var request = new CertificateRequest
        {
            Requester = "integration-test-host",
            CSR = "-----BEGIN CERTIFICATE REQUEST-----\nMIIBWjCCAQQCAQAwHDEaMBgGA1UEAwwRaW50ZWdyYXRpb24tdGVzdC5sb2NhbDAq\n-----END CERTIFICATE REQUEST-----",
            TemplateName = "WebServer"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/certificates/request", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var payload = await response.Content.ReadFromJsonAsync<RequestStatusResponse>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be("Pending");
        payload.RequestId.Should().NotBeEmpty();

        var fetchResponse = await _client.GetAsync($"/api/certificates/requests/{payload.RequestId}");
        fetchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await fetchResponse.Content.ReadFromJsonAsync<CertificateRequest>();
        stored.Should().NotBeNull();
        stored!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task RequestCertificate_WithMissingRequiredFields_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new CertificateRequest
        {
            Requester = "",
            CSR = "",
            TemplateName = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/certificates/request", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        error.Should().NotBeNull();
        error!.Error.Should().Be("ValidationError");
    }

    private sealed class RequestStatusResponse
    {
        public Guid RequestId { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? CertificateId { get; set; }
    }
}

using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using EnterprisePKI.Shared.Models;
using EnterprisePKI.Portal.Controllers;
using EnterprisePKI.Portal.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace Portal.Tests;

public class GatewayServiceTests
{
    private readonly Mock<ILogger<GatewayService>> _loggerMock;

    public GatewayServiceTests()
    {
        _loggerMock = new Mock<ILogger<GatewayService>>();
    }

    [Fact]
    public async Task RequestIssuanceAsync_Success_ReturnsCertificate()
    {
        // Arrange
        var expectedCert = Helpers.CreateTestCertificate(commonName: "issued-cert.example.com");
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expectedCert)
            });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5001") };
        var service = new GatewayService(httpClient, _loggerMock.Object);
        var request = new CertificateRequest
        {
            Id = Guid.NewGuid(),
            CSR = "test-csr",
            TemplateName = "WebServer",
            Requester = "admin"
        };

        // Act
        var result = await service.RequestIssuanceAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.CommonName.Should().Be("issued-cert.example.com");
    }

    [Fact]
    public async Task RequestIssuanceAsync_GatewayError_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5001") };
        var service = new GatewayService(httpClient, _loggerMock.Object);
        var request = new CertificateRequest
        {
            Id = Guid.NewGuid(),
            CSR = "test-csr",
            TemplateName = "WebServer",
            Requester = "admin"
        };

        // Act
        var result = await service.RequestIssuanceAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RequestIssuanceAsync_HttpException_ReturnsNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(exception: new HttpRequestException("Connection refused"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5001") };
        var service = new GatewayService(httpClient, _loggerMock.Object);
        var request = new CertificateRequest
        {
            Id = Guid.NewGuid(),
            CSR = "test-csr",
            TemplateName = "WebServer",
            Requester = "admin"
        };

        // Act
        var result = await service.RequestIssuanceAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RequestIssuanceAsync_WithConfiguredGatewayToken_SendsBearerAuthorizationHeader()
    {
        // Arrange
        var expectedCert = Helpers.CreateTestCertificate(commonName: "issued-cert.example.com");
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(expectedCert)
            });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5001") };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "gateway-service-token");
        var service = new GatewayService(httpClient, _loggerMock.Object);

        var request = new CertificateRequest
        {
            Id = Guid.NewGuid(),
            CSR = "test-csr",
            TemplateName = "WebServer",
            Requester = "admin"
        };

        // Act
        _ = await service.RequestIssuanceAsync(request);

        // Assert
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Authorization.Should().NotBeNull();
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("gateway-service-token");
    }
}

/// <summary>
/// Mock HttpMessageHandler for testing HttpClient-based services.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage? _response;
    private readonly Exception? _exception;
    public HttpRequestMessage? LastRequest { get; private set; }

    public MockHttpMessageHandler(HttpResponseMessage? response = null, Exception? exception = null)
    {
        _response = response;
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        if (_exception != null)
            throw _exception;
        return Task.FromResult(_response!);
    }
}

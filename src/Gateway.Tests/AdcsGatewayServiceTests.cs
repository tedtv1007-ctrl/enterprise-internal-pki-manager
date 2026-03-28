using Moq;
using FluentAssertions;
using EnterprisePKI.Shared.Models;
using EnterprisePKI.Gateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace Gateway.Tests;

public class AdcsGatewayServiceTests
{
    private readonly Mock<ILogger<AdcsGatewayService>> _loggerMock;

    public AdcsGatewayServiceTests()
    {
        _loggerMock = new Mock<ILogger<AdcsGatewayService>>();
    }

    private IConfiguration CreateConfig(string proxyUrl = "")
    {
        var configData = new Dictionary<string, string?>
        {
            { "AdcsProxy:Url", proxyUrl }
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public async Task IssueCertificateAsync_NoProxy_ReturnsMockCertificate()
    {
        // Arrange
        var config = CreateConfig(); // empty proxy URL
        var handler = new MockHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var service = new AdcsGatewayService(_loggerMock.Object, httpClient, config);

        // Act
        var cert = await service.IssueCertificateAsync("test-csr", "WebServer");

        // Assert
        cert.Should().NotBeNull();
        cert.CommonName.Should().Be("mock-cert.enterprise.local");
        cert.Algorithm.Should().Be("RSA-2048");
        cert.KeySize.Should().Be(2048);
        cert.IsPQC.Should().BeFalse();
        cert.IssuerDN.Should().Contain("Mock CA");
    }

    [Fact]
    public async Task IssueCertificateAsync_NoProxy_SetsTimestamps()
    {
        // Arrange
        var config = CreateConfig();
        var handler = new MockHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var service = new AdcsGatewayService(_loggerMock.Object, httpClient, config);

        // Act
        var before = DateTime.UtcNow;
        var cert = await service.IssueCertificateAsync("csr", "Template");
        var after = DateTime.UtcNow;

        // Assert
        cert.NotBefore.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        cert.NotAfter.Should().BeAfter(cert.NotBefore);
        cert.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task RevokeCertificateAsync_ReturnsTrue()
    {
        // Arrange
        var config = CreateConfig();
        var handler = new MockHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var service = new AdcsGatewayService(_loggerMock.Object, httpClient, config);

        // Act
        var result = await service.RevokeCertificateAsync("SN-12345", 1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IssueCertificateAsync_NoProxy_EachCallGeneratesUniqueSerialNumber()
    {
        // Arrange
        var config = CreateConfig();
        var handler = new MockHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var service = new AdcsGatewayService(_loggerMock.Object, httpClient, config);

        // Act
        var cert1 = await service.IssueCertificateAsync("csr1", "WebServer");
        var cert2 = await service.IssueCertificateAsync("csr2", "WebServer");

        // Assert
        cert1.SerialNumber.Should().NotBe(cert2.SerialNumber);
        cert1.Thumbprint.Should().NotBe(cert2.Thumbprint);
    }

    [Fact]
    public async Task IssueCertificateAsync_WithProxy_Success_ReturnsCertificateFromProxy()
    {
        // Arrange
        var proxyResponse = new
        {
            SerialNumber = "PROXY-SN-001",
            Thumbprint = "PROXY-THUMB-001",
            CommonName = "proxy-issued.enterprise.local",
            IssuerDN = "CN=Enterprise CA",
            NotBefore = DateTime.UtcNow,
            NotAfter = DateTime.UtcNow.AddYears(1),
            CertificateBase64 = "AAAA"
        };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(proxyResponse)
        };
        var config = CreateConfig("http://windows-proxy:5050");
        var handler = new MockHandler(httpResponse);
        var httpClient = new HttpClient(handler);
        var service = new AdcsGatewayService(_loggerMock.Object, httpClient, config);

        // Act
        var cert = await service.IssueCertificateAsync("real-csr", "WebServer");

        // Assert
        cert.Should().NotBeNull();
        cert.SerialNumber.Should().Be("PROXY-SN-001");
        cert.CommonName.Should().Be("proxy-issued.enterprise.local");
        cert.IssuerDN.Should().Be("CN=Enterprise CA");
    }

    [Fact]
    public async Task IssueCertificateAsync_WithProxy_Failure_FallsBackToMock()
    {
        // Arrange — proxy returns server error → service falls back to mock
        var config = CreateConfig("http://windows-proxy:5050");
        var handler = new MockHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler);
        var service = new AdcsGatewayService(_loggerMock.Object, httpClient, config);

        // Act
        var cert = await service.IssueCertificateAsync("csr", "WebServer");

        // Assert — should get mock cert since proxy failed
        cert.Should().NotBeNull();
        cert.CommonName.Should().Be("mock-cert.enterprise.local");
    }
}

public class MockHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage? _response;
    private readonly Exception? _exception;

    public MockHandler(HttpResponseMessage? response = null, Exception? exception = null)
    {
        _response = response;
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_exception != null) throw _exception;
        return Task.FromResult(_response!);
    }
}

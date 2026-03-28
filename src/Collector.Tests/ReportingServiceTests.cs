using System.Net;
using System.Net.Http.Json;
using EnterprisePKI.Collector.Services;
using EnterprisePKI.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Collector.Tests;

public class ReportingServiceTests
{
    private readonly Mock<ILogger<ReportingService>> _loggerMock = new();

    private static HttpClient CreateHttpClient(HttpResponseMessage response)
    {
        var handler = new MockHandler(response);
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
    }

    private static HttpClient CreateHttpClient(Exception exception)
    {
        var handler = new MockHandler(exception: exception);
        return new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
    }

    [Fact]
    public async Task ReportDiscoveryAsync_Success_CompletesWithoutException()
    {
        // Arrange
        var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK));
        var service = new ReportingService(httpClient, _loggerMock.Object);
        var report = new DiscoveryReport
        {
            Hostname = "web01.internal",
            Certificates = new List<CertificateDiscovery>
            {
                new() { Thumbprint = "abc123", CommonName = "web01.internal", NotBefore = DateTime.UtcNow, NotAfter = DateTime.UtcNow.AddYears(1) }
            }
        };

        // Act — should not throw
        await service.ReportDiscoveryAsync(report);
    }

    [Fact]
    public async Task ReportDiscoveryAsync_ServerError_DoesNotThrow()
    {
        // Arrange
        var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var service = new ReportingService(httpClient, _loggerMock.Object);
        var report = new DiscoveryReport { Hostname = "web01.internal", Certificates = new() };

        // Act — error responses should be caught and logged, not thrown
        var act = () => service.ReportDiscoveryAsync(report);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReportDiscoveryAsync_HttpException_DoesNotThrow()
    {
        // Arrange
        var httpClient = CreateHttpClient(new HttpRequestException("Connection refused"));
        var service = new ReportingService(httpClient, _loggerMock.Object);
        var report = new DiscoveryReport { Hostname = "web01.internal", Certificates = new() };

        // Act
        var act = () => service.ReportDiscoveryAsync(report);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SubmitRequestAsync_Success_ReturnsRequestId()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new { RequestId = requestId, Status = "Pending" })
        };
        var httpClient = CreateHttpClient(response);
        var service = new ReportingService(httpClient, _loggerMock.Object);
        var request = new CertificateRequest
        {
            Requester = "admin",
            CSR = "test-csr",
            TemplateName = "WebServer"
        };

        // Act
        var result = await service.SubmitRequestAsync(request);

        // Assert
        result.Should().Be(requestId);
    }

    [Fact]
    public async Task SubmitRequestAsync_ServerError_ReturnsNull()
    {
        // Arrange
        var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var service = new ReportingService(httpClient, _loggerMock.Object);
        var request = new CertificateRequest { Requester = "admin", CSR = "csr", TemplateName = "T" };

        // Act
        var result = await service.SubmitRequestAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SubmitRequestAsync_HttpException_ReturnsNull()
    {
        // Arrange
        var httpClient = CreateHttpClient(new HttpRequestException("Connection refused"));
        var service = new ReportingService(httpClient, _loggerMock.Object);
        var request = new CertificateRequest { Requester = "admin", CSR = "csr", TemplateName = "T" };

        // Act
        var result = await service.SubmitRequestAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPendingJobsAsync_Success_ReturnsJobs()
    {
        // Arrange
        var jobs = new List<DeploymentJob>
        {
            new() { Id = Guid.NewGuid(), TargetHostname = "web01", Status = "Pending" }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(jobs)
        };
        var httpClient = CreateHttpClient(response);
        var service = new ReportingService(httpClient, _loggerMock.Object);

        // Act
        var result = await service.GetPendingJobsAsync("web01");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPendingJobsAsync_ServerError_ReturnsEmptyList()
    {
        // Arrange
        var httpClient = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var service = new ReportingService(httpClient, _loggerMock.Object);

        // Act
        var result = await service.GetPendingJobsAsync("web01");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateJobStatusAsync_HttpException_DoesNotThrow()
    {
        // Arrange
        var httpClient = CreateHttpClient(new HttpRequestException("Timeout"));
        var service = new ReportingService(httpClient, _loggerMock.Object);

        // Act
        var act = () => service.UpdateJobStatusAsync(Guid.NewGuid(), "Completed");

        // Assert
        await act.Should().NotThrowAsync();
    }

    private class MockHandler : HttpMessageHandler
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
}

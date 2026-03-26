using EnterprisePKI.Collector.Services;
using EnterprisePKI.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;

namespace Collector.Tests;

public class CertificateRequestServiceTests
{
    [Fact]
    public void GenerateCsr_ShouldReturnPemFormattedCsr()
    {
        // Arrange
        var reporting = CreateReportingServiceForResponse(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = new CertificateRequestService(reporting);

        // Act
        var csr = sut.GenerateCsr("test.internal.example.com");

        // Assert
        csr.Should().StartWith("-----BEGIN CERTIFICATE REQUEST-----");
        csr.Should().Contain("\n");
        csr.Should().EndWith("-----END CERTIFICATE REQUEST-----");
    }

    [Fact]
    public async Task CreateAndSubmitRequestAsync_WhenPortalReturnsId_ShouldReturnId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { RequestId = expectedId })
        };

        var reporting = CreateReportingServiceForResponse(response);
        var sut = new CertificateRequestService(reporting);

        // Act
        var result = await sut.CreateAndSubmitRequestAsync("host01.internal.example.com", "WebServer");

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateAndSubmitRequestAsync_WhenPortalFails_ShouldReturnNull()
    {
        // Arrange
        var reporting = CreateReportingServiceForResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = new CertificateRequestService(reporting);

        // Act
        var result = await sut.CreateAndSubmitRequestAsync("host01.internal.example.com", "WebServer");

        // Assert
        result.Should().BeNull();
    }

    private static ReportingService CreateReportingServiceForResponse(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5050/")
        };
        var logger = new Mock<ILogger<ReportingService>>();
        return new ReportingService(client, logger.Object);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}

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

    [Fact]
    public async Task CreateAndSubmitRequestAsync_WhenCommonNameIsMissing_ShouldReturnNullWithoutCallingPortal()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var handler = new CountingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { RequestId = expectedId })
        });
        var reporting = CreateReportingServiceForHandler(handler);
        var sut = new CertificateRequestService(reporting);

        // Act
        var result = await sut.CreateAndSubmitRequestAsync("", "WebServer");

        // Assert
        result.Should().BeNull();
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAndSubmitRequestAsync_WhenTemplateNameIsMissing_ShouldReturnNullWithoutCallingPortal()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var handler = new CountingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { RequestId = expectedId })
        });
        var reporting = CreateReportingServiceForHandler(handler);
        var sut = new CertificateRequestService(reporting);

        // Act
        var result = await sut.CreateAndSubmitRequestAsync("host01.internal.example.com", " ");

        // Assert
        result.Should().BeNull();
        handler.CallCount.Should().Be(0);
    }

    private static ReportingService CreateReportingServiceForResponse(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);
        return CreateReportingServiceForHandler(handler);
    }

    private static ReportingService CreateReportingServiceForHandler(HttpMessageHandler handler)
    {
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

    private sealed class CountingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public int CallCount { get; private set; }

        public CountingHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_response);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using EnterprisePKI.Gateway;
using EnterprisePKI.Shared.Interfaces;
using EnterprisePKI.Shared.Models;
using EnterprisePKI.Gateway.Controllers;

namespace Gateway.Tests;

public class CaControllerTests
{
    private static Mock<IGatewayIssueRequestThrottle> CreateAllowThrottle()
    {
        var throttle = new Mock<IGatewayIssueRequestThrottle>();
        throttle.Setup(t => t.TryAcquire(It.IsAny<string>())).Returns(true);
        return throttle;
    }

    [Fact]
    public async Task Issue_ReturnsOkWithCertificate()
    {
        // Arrange
        var expectedCert = new Certificate
        {
            Id = Guid.NewGuid(),
            CommonName = "gateway-issued.example.com",
            SerialNumber = "SN-12345678",
            Thumbprint = "abc123",
            IssuerDN = "CN=Test CA",
            NotBefore = DateTime.UtcNow,
            NotAfter = DateTime.UtcNow.AddYears(1),
            Algorithm = "RSA-2048",
            KeySize = 2048
        };

        var mockCaService = new Mock<ICertificateAuthority>();
        mockCaService.Setup(s => s.IssueCertificateAsync("test-csr", "WebServer"))
            .ReturnsAsync(expectedCert);

        var controller = new CaController(mockCaService.Object, CreateAllowThrottle().Object);
        var request = new CaController.IssueRequest { Csr = "test-csr", TemplateName = "WebServer" };

        // Act
        var result = await controller.Issue(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var cert = okResult.Value.Should().BeOfType<Certificate>().Subject;
        cert.CommonName.Should().Be("gateway-issued.example.com");
        cert.SerialNumber.Should().Be("SN-12345678");
    }

    [Fact]
    public async Task Issue_CallsCaServiceWithCorrectParams()
    {
        // Arrange
        var mockCaService = new Mock<ICertificateAuthority>();
        mockCaService.Setup(s => s.IssueCertificateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Certificate());

        var controller = new CaController(mockCaService.Object, CreateAllowThrottle().Object);
        var request = new CaController.IssueRequest { Csr = "my-csr-data", TemplateName = "PQCWebServer" };

        // Act
        await controller.Issue(request);

        // Assert
        mockCaService.Verify(s => s.IssueCertificateAsync("my-csr-data", "PQCWebServer"), Times.Once);
    }

    [Fact]
    public async Task Issue_EmptyCsr_ReturnsBadRequest()
    {
        // Arrange
        var mockCaService = new Mock<ICertificateAuthority>();
        var controller = new CaController(mockCaService.Object, CreateAllowThrottle().Object);
        var request = new CaController.IssueRequest { Csr = "", TemplateName = "WebServer" };

        // Act
        var result = await controller.Issue(request);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequest.Value.Should().BeOfType<ApiError>().Subject;
        error.Error.Should().Be("ValidationError");
        mockCaService.Verify(s => s.IssueCertificateAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Issue_EmptyTemplateName_ReturnsBadRequest()
    {
        // Arrange
        var mockCaService = new Mock<ICertificateAuthority>();
        var controller = new CaController(mockCaService.Object, CreateAllowThrottle().Object);
        var request = new CaController.IssueRequest { Csr = "valid-csr", TemplateName = "" };

        // Act
        var result = await controller.Issue(request);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequest.Value.Should().BeOfType<ApiError>().Subject;
        error.Error.Should().Be("ValidationError");
    }

    [Fact]
    public async Task Issue_ServiceThrows_ReturnsServerError()
    {
        // Arrange
        var mockCaService = new Mock<ICertificateAuthority>();
        mockCaService.Setup(s => s.IssueCertificateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("CA unavailable"));

        var controller = new CaController(mockCaService.Object, CreateAllowThrottle().Object);
        var request = new CaController.IssueRequest { Csr = "test-csr", TemplateName = "WebServer" };

        // Act
        var result = await controller.Issue(request);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        var error = objectResult.Value.Should().BeOfType<ApiError>().Subject;
        error.Error.Should().Be("InternalError");
    }

    [Fact]
    public async Task Revoke_ValidSerialNumber_ReturnsOk()
    {
        // Arrange
        var mockCaService = new Mock<ICertificateAuthority>();
        mockCaService.Setup(s => s.RevokeCertificateAsync("SN-12345", 1))
            .ReturnsAsync(true);

        var controller = new CaController(mockCaService.Object, CreateAllowThrottle().Object);
        var request = new CaController.RevokeRequest { SerialNumber = "SN-12345", Reason = 1 };

        // Act
        var result = await controller.Revoke(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        mockCaService.Verify(s => s.RevokeCertificateAsync("SN-12345", 1), Times.Once);
    }

    [Fact]
    public async Task Revoke_EmptySerialNumber_ReturnsBadRequest()
    {
        // Arrange
        var mockCaService = new Mock<ICertificateAuthority>();
        var controller = new CaController(mockCaService.Object, CreateAllowThrottle().Object);
        var request = new CaController.RevokeRequest { SerialNumber = "", Reason = 0 };

        // Act
        var result = await controller.Revoke(request);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequest.Value.Should().BeOfType<ApiError>().Subject;
        error.Error.Should().Be("ValidationError");
        mockCaService.Verify(s => s.RevokeCertificateAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Revoke_ServiceReturnsFalse_ReturnsNotFound()
    {
        // Arrange
        var mockCaService = new Mock<ICertificateAuthority>();
        mockCaService.Setup(s => s.RevokeCertificateAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(false);

        var controller = new CaController(mockCaService.Object, CreateAllowThrottle().Object);
        var request = new CaController.RevokeRequest { SerialNumber = "SN-NONEXISTENT", Reason = 0 };

        // Act
        var result = await controller.Revoke(request);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFound.Value.Should().BeOfType<ApiError>().Subject;
        error.Error.Should().Be("NotFound");
    }
}

using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using EnterprisePKI.Shared.Interfaces;
using EnterprisePKI.Shared.Models;
using EnterprisePKI.Gateway.Controllers;

namespace Gateway.Tests;

public class CaControllerTests
{
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

        var controller = new CaController(mockCaService.Object);
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

        var controller = new CaController(mockCaService.Object);
        var request = new CaController.IssueRequest { Csr = "my-csr-data", TemplateName = "PQCWebServer" };

        // Act
        await controller.Issue(request);

        // Assert
        mockCaService.Verify(s => s.IssueCertificateAsync("my-csr-data", "PQCWebServer"), Times.Once);
    }
}

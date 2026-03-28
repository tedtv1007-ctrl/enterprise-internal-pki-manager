using EnterprisePKI.Portal.Controllers;
using EnterprisePKI.Shared.Models;
using FluentAssertions;

namespace Portal.Tests;

public class DiscoveredCertificateMapperTests
{
    [Fact]
    public void ToUnmanagedCertificate_ShouldMapDiscoveryValidityWindow()
    {
        // Arrange
        var certificateId = Guid.NewGuid();
        var discovery = new CertificateDiscovery
        {
            Thumbprint = "abc123",
            CommonName = "discovered.internal.example.com",
            NotBefore = DateTime.UtcNow.AddDays(-14),
            NotAfter = DateTime.UtcNow.AddDays(120),
            StoreLocation = "My/LocalMachine"
        };

        // Act
        var certificate = DiscoveredCertificateMapper.ToUnmanagedCertificate(certificateId, discovery);

        // Assert
        certificate.Id.Should().Be(certificateId);
        certificate.NotBefore.Should().Be(discovery.NotBefore);
        certificate.NotAfter.Should().Be(discovery.NotAfter);
        certificate.SerialNumber.Should().Be("DISCOVERED-abc123");
        certificate.Status.Should().Be("Discovered");
    }
}
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EnterprisePKI.Collector.Services;
using FluentAssertions;

namespace Collector.Tests;

public class CertificateDiscoveryMapperTests
{
    [Fact]
    public void FromX509Certificate_ShouldPreserveCertificateValidityWindow()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=mapper-test.internal.example.com",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var notBefore = DateTimeOffset.UtcNow.AddDays(-7);
        var notAfter = DateTimeOffset.UtcNow.AddDays(90);
        using var certificate = request.CreateSelfSigned(notBefore, notAfter);

        // Act
        var discovery = CertificateDiscoveryMapper.FromX509Certificate(certificate, StoreLocation.LocalMachine);

        // Assert
        discovery.NotBefore.Should().Be(certificate.NotBefore);
        discovery.NotAfter.Should().Be(certificate.NotAfter);
        discovery.StoreLocation.Should().Be("Windows Store: LocalMachine/My");
    }
}
using EnterprisePKI.Collector.Services;
using EnterprisePKI.Shared.Models;
using FluentAssertions;

namespace Collector.Tests;

public class WindowsDeploymentServiceTests
{
    [Fact]
    public async Task InstallCertificateAsync_WhenPfxDataMissing_ShouldReturnFalse()
    {
        // Arrange
        var sut = new WindowsDeploymentService();
        var job = new DeploymentJob
        {
            CertificateId = Guid.NewGuid(),
            StoreLocation = "My/LocalMachine",
            PfxData = string.Empty,
            PfxPassword = "irrelevant"
        };

        // Act
        var result = await sut.InstallCertificateAsync(job);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task InstallCertificateAsync_WhenPfxDataInvalidBase64_ShouldReturnFalse()
    {
        // Arrange
        var sut = new WindowsDeploymentService();
        var job = new DeploymentJob
        {
            CertificateId = Guid.NewGuid(),
            StoreLocation = "My/LocalMachine",
            PfxData = "not-a-valid-base64",
            PfxPassword = "irrelevant"
        };

        // Act
        var result = await sut.InstallCertificateAsync(job);

        // Assert
        result.Should().BeFalse();
    }
}

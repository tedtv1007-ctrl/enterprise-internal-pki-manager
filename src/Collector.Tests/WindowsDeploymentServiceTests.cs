using EnterprisePKI.Collector.Services;
using EnterprisePKI.Shared.Models;
using FluentAssertions;
using System.Security.Cryptography.X509Certificates;

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

    [Fact]
    public void TryParseStoreLocation_WithValidInput_ShouldParseExpectedTarget()
    {
        // Act
        var parsed = WindowsDeploymentService.TryParseStoreLocation(
            "My/LocalMachine",
            out var storeName,
            out var storeLocation);

        // Assert
        parsed.Should().BeTrue();
        storeName.Should().Be(StoreName.My);
        storeLocation.Should().Be(StoreLocation.LocalMachine);
    }

    [Fact]
    public void TryParseStoreLocation_WithInvalidInput_ShouldReturnFalse()
    {
        // Act
        var parsed = WindowsDeploymentService.TryParseStoreLocation(
            "invalid-format",
            out _,
            out _);

        // Assert
        parsed.Should().BeFalse();
    }
}

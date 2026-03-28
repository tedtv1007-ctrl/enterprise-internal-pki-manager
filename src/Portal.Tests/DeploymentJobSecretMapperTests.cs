using EnterprisePKI.Portal.Security;
using EnterprisePKI.Shared.Models;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;

namespace Portal.Tests;

public class DeploymentJobSecretMapperTests
{
    [Fact]
    public void ForStorage_ShouldProtectPfxDataAndPassword()
    {
        // Arrange
        var protector = CreateProtector();
        var input = new DeploymentJob
        {
            Id = Guid.NewGuid(),
            CertificateId = Guid.NewGuid(),
            TargetHostname = "host01.internal",
            StoreLocation = "My/LocalMachine",
            Status = "Pending",
            PfxData = "plain-pfx-data",
            PfxPassword = "plain-password"
        };

        // Act
        var secured = DeploymentJobSecretMapper.ForStorage(input, protector);

        // Assert
        secured.PfxData.Should().NotBeNullOrWhiteSpace();
        secured.PfxPassword.Should().NotBeNullOrWhiteSpace();
        secured.PfxData.Should().NotBe("plain-pfx-data");
        secured.PfxPassword.Should().NotBe("plain-password");
    }

    [Fact]
    public void ForCollector_ShouldUnprotectPfxDataAndPassword()
    {
        // Arrange
        var protector = CreateProtector();
        var input = new DeploymentJob
        {
            Id = Guid.NewGuid(),
            CertificateId = Guid.NewGuid(),
            TargetHostname = "host01.internal",
            StoreLocation = "My/LocalMachine",
            Status = "Pending",
            PfxData = "plain-pfx-data",
            PfxPassword = "plain-password"
        };

        var secured = DeploymentJobSecretMapper.ForStorage(input, protector);

        // Act
        var collectorView = DeploymentJobSecretMapper.ForCollector(secured, protector);

        // Assert
        collectorView.PfxData.Should().Be("plain-pfx-data");
        collectorView.PfxPassword.Should().Be("plain-password");
    }

    [Fact]
    public void ForUiList_ShouldRemoveSensitiveMaterial()
    {
        // Arrange
        var job = new DeploymentJob
        {
            Id = Guid.NewGuid(),
            CertificateId = Guid.NewGuid(),
            TargetHostname = "host01.internal",
            StoreLocation = "My/LocalMachine",
            Status = "Pending",
            PfxData = "encrypted-or-plain-data",
            PfxPassword = "encrypted-or-plain-password"
        };

        // Act
        var uiView = DeploymentJobSecretMapper.ForUiList(job);

        // Assert
        uiView.PfxData.Should().BeNull();
        uiView.PfxPassword.Should().BeNull();
    }

    private static IDataProtectorFacade CreateProtector()
    {
        var provider = DataProtectionProvider.Create("portal-tests");
        return new DataProtectorFacade(provider.CreateProtector("deployment-secrets"));
    }
}
using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using EnterprisePKI.Shared.Models;
using EnterprisePKI.Portal.Controllers;
using Microsoft.Extensions.Configuration;

namespace Portal.Tests;

public class Helpers
{
    public static IConfiguration CreateMockConfiguration(string connString = "Host=localhost;Database=test_pki;Username=test;Password=test")
    {
        var configData = new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", connString }
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    public static Certificate CreateTestCertificate(
        Guid? id = null,
        string commonName = "test.example.com",
        string status = "Active",
        bool isPqc = false)
    {
        return new Certificate
        {
            Id = id ?? Guid.NewGuid(),
            CommonName = commonName,
            SerialNumber = $"SN-{Guid.NewGuid().ToString("N")[..8]}",
            Thumbprint = Guid.NewGuid().ToString("N"),
            IssuerDN = "CN=Test CA",
            NotBefore = DateTime.UtcNow.AddDays(-30),
            NotAfter = DateTime.UtcNow.AddDays(335),
            Algorithm = isPqc ? "Dilithium3" : "RSA",
            KeySize = isPqc ? 2592 : 2048,
            IsPQC = isPqc,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static DeploymentJob CreateTestDeploymentJob(
        Guid? id = null,
        string status = "Pending",
        string targetHostname = "test-server-01")
    {
        return new DeploymentJob
        {
            Id = id ?? Guid.NewGuid(),
            CertificateId = Guid.NewGuid(),
            TargetHostname = targetHostname,
            StoreLocation = "My/LocalMachine",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }
}

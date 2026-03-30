using System;
using System.Security.Cryptography.X509Certificates;
using EnterprisePKI.Shared.Models;
using Microsoft.Extensions.Logging;

namespace EnterprisePKI.Collector.Services
{
    public interface IDeploymentService
    {
        Task<bool> InstallCertificateAsync(DeploymentJob job);
    }

    public class WindowsDeploymentService : IDeploymentService
    {
        private readonly ILogger<WindowsDeploymentService> _logger;

        public WindowsDeploymentService(ILogger<WindowsDeploymentService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> InstallCertificateAsync(DeploymentJob job)
        {
            try
            {
                if (string.IsNullOrEmpty(job.PfxData))
                    throw new ArgumentException("PFX data is missing");

                if (!TryParseStoreLocation(job.StoreLocation, out var storeName, out var storeLocation))
                    throw new ArgumentException("StoreLocation must be in the form '<StoreName>/<StoreLocation>'");

                byte[] certData = Convert.FromBase64String(job.PfxData);
                
                // In a real scenario, we'd handle PQC certs differently if the OS doesn't support them in the standard store.
                // For now, we assume standard X509 store for demo purposes.
                using var cert = X509CertificateLoader.LoadPkcs12(
                    certData,
                    job.PfxPassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

                using var store = new X509Store(storeName, storeLocation);
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
                store.Close();

                _logger.LogInformation("Installed certificate {CertificateId} to {StoreLocation}", job.CertificateId, job.StoreLocation);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error installing certificate {CertificateId}", job.CertificateId);
                return false;
            }
        }

        public static bool TryParseStoreLocation(string? rawValue, out StoreName storeName, out StoreLocation storeLocation)
        {
            storeName = default;
            storeLocation = default;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            var parts = rawValue.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            var parsedStoreName = Enum.TryParse(parts[0], true, out storeName);
            var parsedStoreLocation = Enum.TryParse(parts[1], true, out storeLocation);
            return parsedStoreName && parsedStoreLocation;
        }
    }
}

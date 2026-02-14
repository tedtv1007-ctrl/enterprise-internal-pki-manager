using System;
using System.Security.Cryptography.X509Certificates;
using EnterprisePKI.Shared.Models;

namespace EnterprisePKI.Collector.Services
{
    public interface IDeploymentService
    {
        Task<bool> InstallCertificateAsync(DeploymentJob job);
    }

    public class WindowsDeploymentService : IDeploymentService
    {
        public async Task<bool> InstallCertificateAsync(DeploymentJob job)
        {
            try
            {
                if (string.IsNullOrEmpty(job.PfxData))
                    throw new ArgumentException("PFX data is missing");

                byte[] certData = Convert.FromBase64String(job.PfxData);
                
                // In a real scenario, we'd handle PQC certs differently if the OS doesn't support them in the standard store.
                // For now, we assume standard X509 store for demo purposes.
                using var cert = new X509Certificate2(certData, job.PfxPassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

                // Parse StoreLocation: "My/LocalMachine" -> StoreName.My, StoreLocation.LocalMachine
                var parts = job.StoreLocation.Split('/');
                var storeName = StoreName.My;
                var storeLocation = StoreLocation.LocalMachine;

                if (parts.Length == 2)
                {
                    Enum.TryParse(parts[0], true, out storeName);
                    Enum.TryParse(parts[1], true, out storeLocation);
                }

                using var store = new X509Store(storeName, storeLocation);
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
                store.Close();

                Console.WriteLine($"[Deployment] Installed certificate {job.CertificateId} to {job.StoreLocation}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Deployment] Error installing certificate: {ex.Message}");
                return false;
            }
        }
    }
}

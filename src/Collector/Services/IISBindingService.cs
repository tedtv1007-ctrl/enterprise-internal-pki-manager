using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Web.Administration;
using EnterprisePKI.Shared.Models;

namespace EnterprisePKI.Collector.Services
{
    public interface IIISBindingService
    {
        Task<bool> UpdateBindingAsync(string siteName, int port, string ipAddress, byte[] certificateHash, string storeName);
    }

    public class IISBindingService : IIISBindingService
    {
        public async Task<bool> UpdateBindingAsync(string siteName, int port, string ipAddress, byte[] certificateHash, string storeName)
        {
            try
            {
                using var serverManager = new ServerManager();
                var site = serverManager.Sites[siteName];
                if (site == null)
                {
                    Console.WriteLine($"[IIS] Site '{siteName}' not found.");
                    return false;
                }

                string bindingInformation = $"{ipAddress}:{port}:";
                var existingBinding = site.Bindings.FirstOrDefault(b => b.BindingInformation == bindingInformation && b.Protocol == "https");

                if (existingBinding != null)
                {
                    Console.WriteLine($"[IIS] Updating existing binding for {bindingInformation}");
                    existingBinding.CertificateHash = certificateHash;
                    existingBinding.CertificateStoreName = storeName;
                }
                else
                {
                    Console.WriteLine($"[IIS] Adding new binding for {bindingInformation}");
                    var newBinding = site.Bindings.Add(bindingInformation, certificateHash, storeName);
                    newBinding.Protocol = "https";
                }

                serverManager.CommitChanges();
                Console.WriteLine($"[IIS] Successfully updated binding for site '{siteName}' on port {port}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IIS] Error updating IIS binding: {ex.Message}");
                // Fallback for non-Windows environments or if MWA fails
                return await FallbackUpdateBindingAsync(siteName, port, ipAddress, certificateHash, storeName);
            }
        }

        private async Task<bool> FallbackUpdateBindingAsync(string siteName, int port, string ipAddress, byte[] certificateHash, string storeName)
        {
            // In a real proxy scenario, we might use netsh or appcmd
            Console.WriteLine("[IIS] Attempting fallback using netsh...");
            try
            {
                string hashString = BitConverter.ToString(certificateHash).Replace("-", "");
                string appid = "{4dc3e181-e14b-4a21-b022-59fc669b0914}"; // Example AppID for IIS
                
                // 1. Remove existing SSL cert if any
                // netsh http delete sslcert ipport=0.0.0.0:443
                
                // 2. Add new SSL cert
                // netsh http add sslcert ipport=0.0.0.0:443 certhash=... appid={...}
                
                Console.WriteLine($"[IIS] Fallback command: netsh http add sslcert ipport={ipAddress}:{port} certhash={hashString} appid={appid} certstorename={storeName}");
                
                // This would be executed via Process.Start
                return false; // Still returning false as it's just a demo fallback
            }
            catch
            {
                return false;
            }
        }
    }
}

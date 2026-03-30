using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Web.Administration;
using Microsoft.Extensions.Logging;
using EnterprisePKI.Shared.Models;

namespace EnterprisePKI.Collector.Services
{
    public interface IIISBindingService
    {
        Task<bool> UpdateBindingAsync(string siteName, int port, string ipAddress, byte[] certificateHash, string storeName);
    }

    public class IISBindingService : IIISBindingService
    {
        private readonly ILogger<IISBindingService> _logger;

        public IISBindingService(ILogger<IISBindingService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> UpdateBindingAsync(string siteName, int port, string ipAddress, byte[] certificateHash, string storeName)
        {
            try
            {
                using var serverManager = new ServerManager();
                var site = serverManager.Sites[siteName];
                if (site == null)
                {
                    _logger.LogWarning("IIS site '{SiteName}' not found", siteName);
                    return false;
                }

                string bindingInformation = $"{ipAddress}:{port}:";
                var existingBinding = site.Bindings.FirstOrDefault(b => b.BindingInformation == bindingInformation && b.Protocol == "https");

                if (existingBinding != null)
                {
                    _logger.LogInformation("Updating existing IIS binding for {BindingInfo}", bindingInformation);
                    existingBinding.CertificateHash = certificateHash;
                    existingBinding.CertificateStoreName = storeName;
                }
                else
                {
                    _logger.LogInformation("Adding new IIS binding for {BindingInfo}", bindingInformation);
                    var newBinding = site.Bindings.Add(bindingInformation, certificateHash, storeName);
                    newBinding.Protocol = "https";
                }

                serverManager.CommitChanges();
                _logger.LogInformation("Successfully updated IIS binding for site '{SiteName}' on port {Port}", siteName, port);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating IIS binding for site '{SiteName}'", siteName);
                // Fallback for non-Windows environments or if MWA fails
                return await FallbackUpdateBindingAsync(siteName, port, ipAddress, certificateHash, storeName);
            }
        }

        private async Task<bool> FallbackUpdateBindingAsync(string siteName, int port, string ipAddress, byte[] certificateHash, string storeName)
        {
            // In a real proxy scenario, we might use netsh or appcmd
            _logger.LogWarning("Attempting fallback IIS binding update using netsh for site '{SiteName}'", siteName);
            try
            {
                string hashString = Convert.ToHexString(certificateHash);
                string appid = "{4dc3e181-e14b-4a21-b022-59fc669b0914}"; // Example AppID for IIS
                
                _logger.LogInformation("Fallback command: netsh http add sslcert ipport={IpAddress}:{Port} certhash={Hash} appid={AppId} certstorename={StoreName}",
                    ipAddress, port, hashString, appid, storeName);
                
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

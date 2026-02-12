using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnterprisePKI.Shared.Interfaces;
using EnterprisePKI.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnterprisePKI.Gateway.Services
{
    /// <summary>
    /// Prototype service for ADCS interaction.
    /// In a real Windows environment, this would use CERTCLILib or CertRequest COM interfaces.
    /// On Linux, this might proxy to a Windows worker or use Web Services (CES).
    /// </summary>
    public class AdcsGatewayService : ICertificateAuthority
    {
        private readonly ILogger<AdcsGatewayService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _proxyUrl;

        public AdcsGatewayService(ILogger<AdcsGatewayService> logger, HttpClient httpClient, IConfiguration config)
        {
            _logger = logger;
            _httpClient = httpClient;
            _proxyUrl = config["AdcsProxy:Url"] ?? "";
        }

        public async Task<Certificate> IssueCertificateAsync(string csr, string templateName)
        {
            if (!string.IsNullOrEmpty(_proxyUrl))
            {
                _logger.LogInformation("Forwarding CSR to Windows Proxy at {Url}", _proxyUrl);
                
                var response = await _httpClient.PostAsJsonAsync($"{_proxyUrl}/api/adcs/submit", new {
                    Csr = csr,
                    Template = templateName,
                    CaConfig = "Internal-CA-Config" // Should come from config
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>();
                    // Map result to Certificate model
                    return new Certificate { 
                        SerialNumber = result?.serialNumber ?? "Unknown",
                        CommonName = "Issued via Proxy",
                        CreatedAt = DateTime.UtcNow
                    };
                }
            }

            _logger.LogWarning("No ADCS Proxy configured or call failed. Using mock.");
            
            return new Certificate
            {
                Id = Guid.NewGuid(),
                CommonName = "mock-cert.enterprise.local",
                SerialNumber = "MOCK-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Thumbprint = Guid.NewGuid().ToString("N"),
                IssuerDN = "CN=Enterprise Mock CA, DC=enterprise, DC=local",
                NotBefore = DateTime.UtcNow,
                NotAfter = DateTime.UtcNow.AddYears(1),
                Algorithm = "RSA-2048", // Default for now
                KeySize = 2048,
                IsPQC = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> RevokeCertificateAsync(string serialNumber, int reason)
        {
            _logger.LogInformation("Revoking certificate {SerialNumber} for reason {Reason}", serialNumber, reason);
            // TODO: Implementation for certutil -revoke or COM interface
            await Task.Delay(200);
            return true;
        }
    }
}

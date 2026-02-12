using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnterprisePKI.Shared.Models;
using Microsoft.Extensions.Logging;

namespace EnterprisePKI.Collector.Services
{
    public class ReportingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReportingService> _logger;
        private readonly string _portalUrl;

        public ReportingService(HttpClient httpClient, ILogger<ReportingService> logger, string portalUrl)
        {
            _httpClient = httpClient;
            _logger = logger;
            _portalUrl = portalUrl;
        }

        public async Task ReportDiscoveryAsync(DiscoveryReport report)
        {
            try
            {
                _logger.LogInformation("Reporting {Count} discovered certificates to Portal at {Url}", 
                    report.Certificates.Count, _portalUrl);
                
                var response = await _httpClient.PostAsJsonAsync($"{_portalUrl}/api/certificates/discovery", report);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Discovery report submitted successfully.");
                }
                else
                {
                    _logger.LogError("Failed to submit discovery report. Status: {Status}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reporting discovery findings.");
            }
        }
    }
}

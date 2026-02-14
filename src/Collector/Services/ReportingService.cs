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

        public ReportingService(HttpClient httpClient, ILogger<ReportingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task ReportDiscoveryAsync(DiscoveryReport report)
        {
            try
            {
                _logger.LogInformation("Reporting {Count} discovered certificates to Portal", 
                    report.Certificates.Count);
                
                var response = await _httpClient.PostAsJsonAsync("api/certificates/discovery", report);
                
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

        public async Task<Guid?> SubmitRequestAsync(CertificateRequest request)
        {
            try
            {
                _logger.LogInformation("Submitting certificate request for {CN} to Portal", request.Requester);
                
                var response = await _httpClient.PostAsJsonAsync("api/certificates/request", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<RequestResponse>();
                    _logger.LogInformation("Certificate request submitted successfully. ID: {Id}", result?.RequestId);
                    return result?.RequestId;
                }
                else
                {
                    _logger.LogError("Failed to submit certificate request. Status: {Status}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while submitting certificate request.");
            }
            return null;
        }

        public async Task<IEnumerable<DeploymentJob>> GetPendingJobsAsync(string hostname)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/deployments/jobs/{hostname}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<IEnumerable<DeploymentJob>>() ?? new List<DeploymentJob>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching deployment jobs.");
            }
            return new List<DeploymentJob>();
        }

        public async Task UpdateJobStatusAsync(Guid jobId, string status, string? error = null)
        {
            try
            {
                var update = new { Status = status, ErrorMessage = error };
                await _httpClient.PostAsJsonAsync($"api/deployments/jobs/{jobId}/status", update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating job status.");
            }
        }

        private class RequestResponse
        {
            public Guid RequestId { get; set; }
        }
    }
}

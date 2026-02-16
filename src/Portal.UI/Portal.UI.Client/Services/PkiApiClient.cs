using System.Net.Http.Json;
using EnterprisePKI.Shared.Models;

namespace Portal.UI.Client.Services
{
    public class PkiApiClient
    {
        private readonly HttpClient _http;

        public PkiApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<DashboardStats?> GetStatsAsync()
        {
            return await _http.GetFromJsonAsync<DashboardStats>("api/Dashboard/stats");
        }

        public async Task<List<Certificate>> GetCertificatesAsync()
        {
            return await _http.GetFromJsonAsync<List<Certificate>>("api/Certificates") ?? new();
        }

        public async Task<List<Agent>> GetAgentsAsync()
        {
            return await _http.GetFromJsonAsync<List<Agent>>("api/Agents") ?? new();
        }

        public async Task<List<DeploymentJob>> GetDeploymentJobsAsync(string hostname = "all")
        {
            // Note: The API has GetPendingJobs(hostname). We might need a general list endpoint.
            // For now, let's assume we might add a general one or just mock it.
            return await _http.GetFromJsonAsync<List<DeploymentJob>>($"api/Deployments/jobs/{hostname}") ?? new();
        }
    }

    public class DashboardStats
    {
        public int TotalCertificates { get; set; }
        public int ExpiringSoon { get; set; }
        public int ActiveAgents { get; set; }
        public int PqcReadyCertificates { get; set; }
    }
}

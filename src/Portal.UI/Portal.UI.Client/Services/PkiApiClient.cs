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
            var res = await _http.GetFromJsonAsync<PaginatedResult<Certificate>>("api/Certificates");
            return res?.Items?.ToList() ?? new();
        }

        public async Task<List<Agent>> GetAgentsAsync()
        {
            var res = await _http.GetFromJsonAsync<PaginatedResult<Agent>>("api/Agents");
            return res?.Items?.ToList() ?? new();
        }

        public async Task<List<DeploymentJob>> GetDeploymentJobsAsync()
        {
            var res = await _http.GetFromJsonAsync<PaginatedResult<DeploymentJob>>("api/Deployments/jobs");
            return res?.Items?.ToList() ?? new();
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

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EnterprisePKI.Shared.Models;

namespace EnterprisePKI.Portal.Services
{
    public class GatewayService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GatewayService> _logger;

        public GatewayService(HttpClient httpClient, ILogger<GatewayService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<Certificate?> RequestIssuanceAsync(CertificateRequest request)
        {
            try
            {
                _logger.LogInformation("Forwarding CSR for {Id} to Gateway", request.Id);
                
                var response = await _httpClient.PostAsJsonAsync("api/ca/issue", new {
                    Csr = request.CSR,
                    TemplateName = request.TemplateName
                });

                if (response.IsSuccessStatusCode)
                {
                    var cert = await response.Content.ReadFromJsonAsync<Certificate>();
                    return cert;
                }
                
                _logger.LogError("Gateway returned error: {Status}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gateway");
            }
            return null;
        }
    }
}

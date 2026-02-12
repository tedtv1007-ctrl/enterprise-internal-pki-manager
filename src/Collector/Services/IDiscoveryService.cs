using System.Collections.Generic;
using System.Threading.Tasks;
using EnterprisePKI.Shared.Models;

namespace EnterprisePKI.Collector.Services
{
    public interface IDiscoveryService
    {
        Task<List<CertificateDiscovery>> DiscoverCertificatesAsync();
    }
}

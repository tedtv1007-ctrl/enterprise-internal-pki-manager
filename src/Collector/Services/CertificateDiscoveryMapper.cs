using System.Security.Cryptography.X509Certificates;
using EnterprisePKI.Shared.Models;

namespace EnterprisePKI.Collector.Services;

internal static class CertificateDiscoveryMapper
{
    public static CertificateDiscovery FromX509Certificate(X509Certificate2 certificate, StoreLocation location)
    {
        return new CertificateDiscovery
        {
            Thumbprint = certificate.Thumbprint,
            CommonName = certificate.Subject,
            NotBefore = certificate.NotBefore,
            NotAfter = certificate.NotAfter,
            StoreLocation = $"Windows Store: {location}/My"
        };
    }
}
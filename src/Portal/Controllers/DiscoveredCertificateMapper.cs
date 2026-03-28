using EnterprisePKI.Shared.Models;

namespace EnterprisePKI.Portal.Controllers;

internal static class DiscoveredCertificateMapper
{
    public static Certificate ToUnmanagedCertificate(Guid certificateId, CertificateDiscovery discovery)
    {
        return new Certificate
        {
            Id = certificateId,
            CommonName = discovery.CommonName,
            SerialNumber = $"DISCOVERED-{discovery.Thumbprint}",
            Thumbprint = discovery.Thumbprint,
            IssuerDN = "Unknown",
            NotBefore = discovery.NotBefore,
            NotAfter = discovery.NotAfter,
            Algorithm = "Unknown",
            KeySize = 0,
            Status = "Discovered"
        };
    }
}
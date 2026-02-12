using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace EnterprisePKI.Collector.Services
{
    public class CertificateRequestService
    {
        public string GenerateCsr(string commonName)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                $"CN={commonName}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Add SANs or other extensions if needed
            // request.CertificateExtensions.Add(...)

            var csr = request.CreateSigningRequest();
            var base64Csr = Convert.ToBase64String(csr);

            return $"-----BEGIN CERTIFICATE REQUEST-----\n{base64Csr}\n-----END CERTIFICATE REQUEST-----";
        }
    }
}

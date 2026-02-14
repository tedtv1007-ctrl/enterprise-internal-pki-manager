using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace EnterprisePKI.Collector.Services
{
    public class CertificateRequestService
    {
        private readonly ReportingService _reportingService;

        public CertificateRequestService(ReportingService reportingService)
        {
            _reportingService = reportingService;
        }

        public string GenerateCsr(string commonName)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                $"CN={commonName}",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var csr = request.CreateSigningRequest();
            var base64Csr = Convert.ToBase64String(csr);

            return $"-----BEGIN CERTIFICATE REQUEST-----\n{base64Csr}\n-----END CERTIFICATE REQUEST-----";
        }

        public async Task<Guid?> CreateAndSubmitRequestAsync(string commonName, string templateName)
        {
            var csr = GenerateCsr(commonName);
            var request = new EnterprisePKI.Shared.Models.CertificateRequest
            {
                Requester = Environment.MachineName,
                CSR = csr,
                TemplateName = templateName,
                Status = "Pending"
            };

            return await _reportingService.SubmitRequestAsync(request);
        }
    }
}

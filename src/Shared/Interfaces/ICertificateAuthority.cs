using System.Threading.Tasks;
using EnterprisePKI.Shared.Models;

namespace EnterprisePKI.Shared.Interfaces
{
    public interface ICertificateAuthority
    {
        /// <summary>
        /// Submits a CSR to the CA and returns the issued certificate.
        /// </summary>
        Task<Certificate> IssueCertificateAsync(string csr, string templateName);
        
        /// <summary>
        /// Revokes a certificate by serial number.
        /// </summary>
        Task<bool> RevokeCertificateAsync(string serialNumber, int reason);
    }
}

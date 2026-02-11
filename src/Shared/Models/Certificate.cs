using System;

namespace EnterprisePKI.Shared.Models
{
    public class Certificate
    {
        public Guid Id { get; set; }
        public string CommonName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Thumbprint { get; set; } = string.Empty;
        public string IssuerDN { get; set; } = string.Empty;
        public DateTime NotBefore { get; set; }
        public DateTime NotAfter { get; set; }
        
        // Crypto-Agility / PQC Ready
        public string Algorithm { get; set; } = string.Empty; // e.g. "RSA-2048", "Dilithium3"
        public int KeySize { get; set; }
        public bool IsPQC { get; set; }
        
        // Binary data for future-proofing (PQC keys are large)
        public byte[]? RawData { get; set; }
        
        public string Status { get; set; } = "Active"; // Active, Revoked, Expired
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

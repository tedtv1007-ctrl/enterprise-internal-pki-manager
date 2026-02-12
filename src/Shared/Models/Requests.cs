using System;
using System.Collections.Generic;

namespace EnterprisePKI.Shared.Models
{
    public class CertificateRequest
    {
        public Guid Id { get; set; }
        public string Requester { get; set; } = string.Empty;
        public string CSR { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Issued
        public Guid? CertificateId { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    public class Endpoint
    {
        public Guid Id { get; set; }
        public string Hostname { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // IIS, F5, K8s, etc.
    }

    public class DiscoveryReport
    {
        public string Hostname { get; set; } = string.Empty;
        public List<CertificateDiscovery> Certificates { get; set; } = new();
    }

    public class CertificateDiscovery
    {
        public string Thumbprint { get; set; } = string.Empty;
        public string StoreLocation { get; set; } = string.Empty; // e.g. "My/LocalMachine", "IIS Site: Default Web Site"
        public string CommonName { get; set; } = string.Empty;
        public DateTime NotAfter { get; set; }
        public string BindingInfo { get; set; } = string.Empty; // e.g. "1.1.1.1:443"
    }
}

using System;

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
}

using System;

namespace EnterprisePKI.Shared.Models
{
    public class DeploymentJob
    {
        public Guid Id { get; set; }
        public Guid CertificateId { get; set; }
        public string TargetHostname { get; set; } = string.Empty;
        public string StoreLocation { get; set; } = string.Empty; // e.g. "My/LocalMachine", "C:\Certs\cert.pfx"
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Failed
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Data for deployment
        public string? PfxData { get; set; } // Base64 encoded PFX (encrypted or password protected)
        public string? PfxPassword { get; set; }
    }
}

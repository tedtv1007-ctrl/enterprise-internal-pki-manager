using System;

namespace EnterprisePKI.Shared.Models
{
    public class Agent
    {
        public Guid Id { get; set; }
        public string Hostname { get; set; } = string.Empty;
        public string? IPAddress { get; set; }
        public string Type { get; set; } = "Windows"; // Windows, Linux, etc.
        public DateTime? LastHeartbeat { get; set; }
        public string Status { get; set; } = "Offline";
    }
}

using Microsoft.AspNetCore.Mvc;
using Dapper;
using Npgsql;
using EnterprisePKI.Shared.Models;
using System.Data;

namespace EnterprisePKI.Portal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            using var db = CreateConnection();
            var totalCerts = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Certificates");
            var expiringSoon = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Certificates WHERE NotAfter < @Threshold", new { Threshold = DateTime.UtcNow.AddDays(30) });
            var activeAgents = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Endpoints WHERE LastHeartbeat > @Threshold", new { Threshold = DateTime.UtcNow.AddMinutes(-5) });
            var pqcCerts = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Certificates WHERE IsPQC = true");

            return Ok(new
            {
                TotalCertificates = totalCerts,
                ExpiringSoon = expiringSoon,
                ActiveAgents = activeAgents,
                PqcReadyCertificates = pqcCerts
            });
        }
    }
}

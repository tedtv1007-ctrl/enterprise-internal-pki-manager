using Microsoft.AspNetCore.Mvc;
using Dapper;
using Npgsql;
using EnterprisePKI.Shared.Models;
using System.Data;

namespace EnterprisePKI.Portal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeploymentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DeploymentsController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        [HttpGet("jobs/{hostname}")]
        public async Task<IActionResult> GetPendingJobs(string hostname)
        {
            using var db = CreateConnection();
            var jobs = await db.QueryAsync<DeploymentJob>(
                "SELECT * FROM DeploymentJobs WHERE TargetHostname = @Hostname AND Status = 'Pending'",
                new { Hostname = hostname });
            
            return Ok(jobs);
        }

        [HttpPost("jobs/{id}/status")]
        public async Task<IActionResult> UpdateJobStatus(Guid id, [FromBody] DeploymentStatusUpdate update)
        {
            using var db = CreateConnection();
            var sql = @"UPDATE DeploymentJobs 
                        SET Status = @Status, ErrorMessage = @ErrorMessage, CompletedAt = @CompletedAt 
                        WHERE Id = @Id";
            
            var completedAt = update.Status == "Completed" ? DateTime.UtcNow : (DateTime?)null;
            
            await db.ExecuteAsync(sql, new { 
                Id = id, 
                Status = update.Status, 
                ErrorMessage = update.ErrorMessage,
                CompletedAt = completedAt
            });

            return Ok();
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateJob(DeploymentJob job)
        {
            job.Id = Guid.NewGuid();
            job.CreatedAt = DateTime.UtcNow;
            job.Status = "Pending";

            using var db = CreateConnection();
            var sql = @"INSERT INTO DeploymentJobs (Id, CertificateId, TargetHostname, StoreLocation, Status, PfxData, PfxPassword, CreatedAt)
                        VALUES (@Id, @CertificateId, @TargetHostname, @StoreLocation, @Status, @PfxData, @PfxPassword, @CreatedAt)";
            
            await db.ExecuteAsync(sql, job);
            return Ok(job);
        }
    }

    public class DeploymentStatusUpdate
    {
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}

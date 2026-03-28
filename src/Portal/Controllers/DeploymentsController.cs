using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Dapper;
using Npgsql;
using EnterprisePKI.Portal.Security;
using EnterprisePKI.Shared.Models;
using System.Data;

namespace EnterprisePKI.Portal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeploymentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly IDataProtectorFacade _protector;
        private readonly ILogger<DeploymentsController> _logger;

        public DeploymentsController(
            IConfiguration configuration,
            IDataProtectorFacade protector,
            ILogger<DeploymentsController> logger)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            _protector = protector;
            _logger = logger;
        }

        private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        [HttpGet("jobs")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            using var db = CreateConnection();
            var totalCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM DeploymentJobs");
            var offset = (page - 1) * pageSize;
            var jobs = await db.QueryAsync<DeploymentJob>(
                "SELECT * FROM DeploymentJobs ORDER BY CreatedAt DESC OFFSET @Offset LIMIT @Limit",
                new { Offset = offset, Limit = pageSize });

            var safeJobs = jobs.Select(DeploymentJobSecretMapper.ForUiList);

            _logger.LogInformation("Returned deployment list page {Page} size {PageSize}", page, pageSize);
            
            return Ok(new PaginatedResult<DeploymentJob>
            {
                Items = safeJobs,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        [HttpGet("jobs/{hostname}")]
        public async Task<IActionResult> GetPendingJobs(string hostname)
        {
            using var db = CreateConnection();
            var jobs = await db.QueryAsync<DeploymentJob>(
                "SELECT * FROM DeploymentJobs WHERE TargetHostname = @Hostname AND Status = 'Pending'",
                new { Hostname = hostname });

            var collectorJobs = jobs.Select(job => DeploymentJobSecretMapper.ForCollector(job, _protector));
            _logger.LogInformation("Returned {Count} pending deployment jobs for host {Hostname}", collectorJobs.Count(), hostname);
            
            return Ok(collectorJobs);
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

            _logger.LogInformation("Deployment job {JobId} status updated to {Status}", id, update.Status);

            return Ok();
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateJob(DeploymentJob job)
        {
            job.Id = Guid.NewGuid();
            job.CreatedAt = DateTime.UtcNow;
            job.Status = "Pending";
            var securedJob = DeploymentJobSecretMapper.ForStorage(job, _protector);

            using var db = CreateConnection();
            var sql = @"INSERT INTO DeploymentJobs (Id, CertificateId, TargetHostname, StoreLocation, Status, PfxData, PfxPassword, CreatedAt)
                        VALUES (@Id, @CertificateId, @TargetHostname, @StoreLocation, @Status, @PfxData, @PfxPassword, @CreatedAt)";
            
            await db.ExecuteAsync(sql, securedJob);

            _logger.LogInformation("Created deployment job {JobId} for certificate {CertificateId}", job.Id, job.CertificateId);

            return Ok(DeploymentJobSecretMapper.ForUiList(job));
        }
    }

    public class DeploymentStatusUpdate
    {
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}

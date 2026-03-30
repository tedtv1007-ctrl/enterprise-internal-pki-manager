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
    [Produces("application/json")]
    public class DeploymentsController : ControllerBase
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Pending",
            "InProgress",
            "Completed",
            "Failed"
        };

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
        [ProducesResponseType(typeof(PaginatedResult<DeploymentJob>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1 || pageSize < 1 || pageSize > 200)
            {
                return BadRequest(new ApiError("ValidationError", "page must be >= 1 and pageSize must be between 1 and 200."));
            }

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

        [HttpGet("jobs/id/{id:guid}")]
        [ProducesResponseType(typeof(DeploymentJob), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetJobById(Guid id)
        {
            using var db = CreateConnection();
            var job = await db.QueryFirstOrDefaultAsync<DeploymentJob>(
                "SELECT * FROM DeploymentJobs WHERE Id = @Id",
                new { Id = id });

            if (job is null)
            {
                return NotFound(new ApiError("NotFound", $"Deployment job {id} was not found."));
            }

            return Ok(DeploymentJobSecretMapper.ForUiList(job));
        }

        [HttpGet("jobs/{hostname}")]
        [ProducesResponseType(typeof(IEnumerable<DeploymentJob>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPendingJobs(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                return BadRequest(new ApiError("ValidationError", "hostname is required."));
            }

            using var db = CreateConnection();
            var jobs = await db.QueryAsync<DeploymentJob>(
                "SELECT * FROM DeploymentJobs WHERE TargetHostname = @Hostname AND Status = 'Pending'",
                new { Hostname = hostname });

            var collectorJobs = jobs.Select(job => DeploymentJobSecretMapper.ForCollector(job, _protector));
            _logger.LogInformation("Returned {Count} pending deployment jobs for host {Hostname}", collectorJobs.Count(), hostname);
            
            return Ok(collectorJobs);
        }

        [HttpPost("jobs/{id}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateJobStatus(Guid id, [FromBody] DeploymentStatusUpdate update)
        {
            if (!AllowedStatuses.Contains(update.Status))
            {
                return BadRequest(new ApiError("ValidationError", "Invalid deployment status value."));
            }

            using var db = CreateConnection();
            var sql = @"UPDATE DeploymentJobs 
                        SET Status = @Status, ErrorMessage = @ErrorMessage, CompletedAt = @CompletedAt 
                        WHERE Id = @Id";
            
            var completedAt = update.Status == "Completed" ? DateTime.UtcNow : (DateTime?)null;
            
            var rows = await db.ExecuteAsync(sql, new {
                Id = id, 
                Status = update.Status, 
                ErrorMessage = update.ErrorMessage,
                CompletedAt = completedAt
            });

            if (rows == 0)
            {
                return NotFound(new ApiError("NotFound", $"Deployment job {id} was not found."));
            }

            _logger.LogInformation("PKI_AUDIT: Deployment job {JobId} status updated to {Status}", id, update.Status);

            return Ok();
        }

        [HttpPost("jobs")]
        [HttpPost("create")]
        [ProducesResponseType(typeof(DeploymentJob), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateJob(DeploymentJob job)
        {
            if (job.CertificateId == Guid.Empty || string.IsNullOrWhiteSpace(job.TargetHostname) || string.IsNullOrWhiteSpace(job.StoreLocation))
            {
                return BadRequest(new ApiError("ValidationError", "CertificateId, TargetHostname, and StoreLocation are required."));
            }

            job.Id = Guid.NewGuid();
            job.CreatedAt = DateTime.UtcNow;
            job.Status = "Pending";
            var securedJob = DeploymentJobSecretMapper.ForStorage(job, _protector);

            using var db = CreateConnection();
            var sql = @"INSERT INTO DeploymentJobs (Id, CertificateId, TargetHostname, StoreLocation, Status, PfxData, PfxPassword, CreatedAt)
                        VALUES (@Id, @CertificateId, @TargetHostname, @StoreLocation, @Status, @PfxData, @PfxPassword, @CreatedAt)";
            
            await db.ExecuteAsync(sql, securedJob);

            var principal = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            _logger.LogInformation("PKI_AUDIT: Deployment job {JobId} created for certificate {CertificateId} targeting {Hostname} by {Principal}",
                job.Id, job.CertificateId, job.TargetHostname, principal);

            var safeJob = DeploymentJobSecretMapper.ForUiList(job);
            return CreatedAtAction(nameof(GetJobById), new { id = job.Id }, safeJob);
        }
    }

    public class DeploymentStatusUpdate
    {
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}

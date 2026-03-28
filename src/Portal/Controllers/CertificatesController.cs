using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Dapper;
using Npgsql;
using EnterprisePKI.Shared.Models;
using System.Data;

namespace EnterprisePKI.Portal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CertificatesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly Services.GatewayService _gatewayService;

        public CertificatesController(IConfiguration configuration, Services.GatewayService gatewayService)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            _gatewayService = gatewayService;
        }

        private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1 || pageSize < 1 || pageSize > 200)
            {
                return BadRequest(new ApiError("ValidationError", "page must be >= 1 and pageSize must be between 1 and 200."));
            }

            using var db = CreateConnection();
            var totalCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Certificates");
            var offset = (page - 1) * pageSize;
            var certs = await db.QueryAsync<Certificate>(
                "SELECT * FROM Certificates ORDER BY NotAfter ASC OFFSET @Offset LIMIT @Limit", 
                new { Offset = offset, Limit = pageSize });
            
            return Ok(new PaginatedResult<Certificate>
            {
                Items = certs,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            using var db = CreateConnection();
            var cert = await db.QueryFirstOrDefaultAsync<Certificate>("SELECT * FROM Certificates WHERE Id = @Id", new { Id = id });
            if (cert == null) return NotFound(new ApiError("NotFound", $"Certificate {id} not found"));
            return Ok(cert);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Certificate cert)
        {
            if (!ModelState.IsValid) return BadRequest(new ApiError("ValidationError", "Invalid certificate data", ModelState));
            cert.Id = Guid.NewGuid();
            cert.CreatedAt = DateTime.UtcNow;
            cert.UpdatedAt = DateTime.UtcNow;

            using var db = CreateConnection();
            var sql = @"INSERT INTO Certificates (Id, CommonName, SerialNumber, Thumbprint, IssuerDN, NotBefore, NotAfter, Algorithm, KeySize, IsPQC, RawData, Status, CreatedAt, UpdatedAt)
                        VALUES (@Id, @CommonName, @SerialNumber, @Thumbprint, @IssuerDN, @NotBefore, @NotAfter, @Algorithm, @KeySize, @IsPQC, @RawData, @Status, @CreatedAt, @UpdatedAt)";
            
            await db.ExecuteAsync(sql, cert);
            return CreatedAtAction(nameof(GetById), new { id = cert.Id }, cert);
        }

        [HttpPost("discovery")]
        public async Task<IActionResult> ReportDiscovery(DiscoveryReport report)
        {
            if (string.IsNullOrWhiteSpace(report.Hostname))
            {
                return BadRequest(new ApiError("ValidationError", "Hostname is required."));
            }

            if (report.Certificates is null || report.Certificates.Count == 0)
            {
                return BadRequest(new ApiError("ValidationError", "At least one discovered certificate is required."));
            }

            using var db = CreateConnection();
            db.Open();
            using var trans = db.BeginTransaction();

            try
            {
                // 1. Ensure Endpoint exists
                var endpointId = await db.QueryFirstOrDefaultAsync<Guid?>(
                    "SELECT Id FROM Endpoints WHERE Hostname = @Hostname", 
                    new { Hostname = report.Hostname }, 
                    transaction: trans);

                if (endpointId == null)
                {
                    endpointId = Guid.NewGuid();
                    await db.ExecuteAsync(
                        "INSERT INTO Endpoints (Id, Hostname, Type) VALUES (@Id, @Hostname, 'Windows')",
                        new { Id = endpointId, Hostname = report.Hostname },
                        transaction: trans);
                }

                // 2. Process discovered certificates
                foreach (var discovered in report.Certificates)
                {
                    // Check if cert exists in our DB
                    var certId = await db.QueryFirstOrDefaultAsync<Guid?>(
                        "SELECT Id FROM Certificates WHERE Thumbprint = @Thumbprint",
                        new { Thumbprint = discovered.Thumbprint },
                        transaction: trans);

                    if (certId == null)
                    {
                        // Record as "Unmanaged" or "Discovered" certificate
                        certId = Guid.NewGuid();
                        var discoveredCertificate = DiscoveredCertificateMapper.ToUnmanagedCertificate(certId.Value, discovered);
                        await db.ExecuteAsync(
                            @"INSERT INTO Certificates (Id, CommonName, SerialNumber, Thumbprint, IssuerDN, NotBefore, NotAfter, Algorithm, KeySize, Status)
                              VALUES (@Id, @CommonName, @SerialNumber, @Thumbprint, @IssuerDN, @NotBefore, @NotAfter, @Algorithm, @KeySize, @Status)",
                            discoveredCertificate,
                            transaction: trans);
                    }

                    // 3. Update/Insert Deployment record
                    await db.ExecuteAsync(
                        @"INSERT INTO CertificateDeployments (CertificateId, EndpointId, LastSeen)
                          VALUES (@CertificateId, @EndpointId, CURRENT_TIMESTAMP)
                          ON CONFLICT (CertificateId, EndpointId) DO UPDATE SET LastSeen = CURRENT_TIMESTAMP",
                        new { CertificateId = certId, EndpointId = endpointId },
                        transaction: trans);
                }

                trans.Commit();
                return Ok(new { Message = "Discovery processed successfully" });
            }
            catch (Exception)
            {
                trans.Rollback();
                // Avoid leaking internal exception details per security-audit skill
                return StatusCode(500, new ApiError("InternalServerError", "An error occurred while processing discovery report."));
            }
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestCertificate(CertificateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Requester)
                || string.IsNullOrWhiteSpace(req.CSR)
                || string.IsNullOrWhiteSpace(req.TemplateName))
            {
                return BadRequest(new ApiError("ValidationError", "Requester, CSR, and TemplateName are required."));
            }

            req.Id = Guid.NewGuid();
            req.RequestedAt = DateTime.UtcNow;
            req.Status = "Pending";

            using var db = CreateConnection();
            var sql = @"INSERT INTO CertificateRequests (Id, Requester, CSR, TemplateName, Status, RequestedAt)
                        VALUES (@Id, @Requester, @CSR, @TemplateName, @Status, @RequestedAt)";
            
            await db.ExecuteAsync(sql, req);

            // Forward to Gateway
            var issuedCert = await _gatewayService.RequestIssuanceAsync(req);
            if (issuedCert != null)
            {
                // Save the certificate
                issuedCert.Id = Guid.NewGuid();
                issuedCert.CreatedAt = DateTime.UtcNow;
                issuedCert.UpdatedAt = DateTime.UtcNow;
                issuedCert.Status = "Active";

                var certSql = @"INSERT INTO Certificates (Id, CommonName, SerialNumber, Thumbprint, IssuerDN, NotBefore, NotAfter, Algorithm, KeySize, IsPQC, RawData, Status, CreatedAt, UpdatedAt)
                            VALUES (@Id, @CommonName, @SerialNumber, @Thumbprint, @IssuerDN, @NotBefore, @NotAfter, @Algorithm, @KeySize, @IsPQC, @RawData, @Status, @CreatedAt, @UpdatedAt)";
                await db.ExecuteAsync(certSql, issuedCert);

                // Update Request Status
                await db.ExecuteAsync("UPDATE CertificateRequests SET Status = 'Issued', CertificateId = @CertId WHERE Id = @RequestId", 
                    new { CertId = issuedCert.Id, RequestId = req.Id });

                return CreatedAtAction(
                    nameof(GetRequestById),
                    new { id = req.Id },
                    new { RequestId = req.Id, Status = "Issued", CertificateId = issuedCert.Id });
            }

            return CreatedAtAction(
                nameof(GetRequestById),
                new { id = req.Id },
                new { RequestId = req.Id, Status = "Pending" });
        }

        [HttpGet("requests/{id:guid}")]
        public async Task<IActionResult> GetRequestById(Guid id)
        {
            using var db = CreateConnection();
            var request = await db.QueryFirstOrDefaultAsync<CertificateRequest>(
                "SELECT * FROM CertificateRequests WHERE Id = @Id",
                new { Id = id });

            if (request is null)
            {
                return NotFound(new ApiError("NotFound", $"Certificate request {id} not found"));
            }

            return Ok(request);
        }
    }
}

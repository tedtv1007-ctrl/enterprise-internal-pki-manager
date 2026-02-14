using Microsoft.AspNetCore.Mvc;
using Dapper;
using Npgsql;
using EnterprisePKI.Shared.Models;
using System.Data;

namespace EnterprisePKI.Portal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        public async Task<IActionResult> GetAll()
        {
            using var db = CreateConnection();
            var certs = await db.QueryAsync<Certificate>("SELECT * FROM Certificates ORDER BY NotAfter ASC");
            return Ok(certs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            using var db = CreateConnection();
            var cert = await db.QueryFirstOrDefaultAsync<Certificate>("SELECT * FROM Certificates WHERE Id = @Id", new { Id = id });
            if (cert == null) return NotFound();
            return Ok(cert);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Certificate cert)
        {
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
                        await db.ExecuteAsync(
                            @"INSERT INTO Certificates (Id, CommonName, SerialNumber, Thumbprint, IssuerDN, NotBefore, NotAfter, Algorithm, KeySize, Status)
                              VALUES (@Id, @CommonName, 'DISCOVERED', @Thumbprint, 'Unknown', @NotAfter, @NotAfter, 'Unknown', 0, 'Discovered')",
                            new { 
                                Id = certId, 
                                CommonName = discovered.CommonName, 
                                Thumbprint = discovered.Thumbprint,
                                NotAfter = discovered.NotAfter
                            },
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
            catch (Exception ex)
            {
                trans.Rollback();
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestCertificate(CertificateRequest req)
        {
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
                
                return Ok(new { RequestId = req.Id, Status = "Issued", CertificateId = issuedCert.Id });
            }
            
            return Ok(new { RequestId = req.Id, Status = "Pending" });
        }
    }
}

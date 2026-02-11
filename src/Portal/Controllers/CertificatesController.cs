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

        public CertificatesController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
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
    }
}

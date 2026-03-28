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
    public class AgentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public AgentsController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            using var db = CreateConnection();
            var totalCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Endpoints");
            var offset = (page - 1) * pageSize;
            var agents = await db.QueryAsync<Agent>(
                "SELECT * FROM Endpoints ORDER BY LastHeartbeat DESC OFFSET @Offset LIMIT @Limit",
                new { Offset = offset, Limit = pageSize });
                
            return Ok(new PaginatedResult<Agent>
            {
                Items = agents,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }
    }
}

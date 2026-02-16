using Microsoft.AspNetCore.Mvc;
using Dapper;
using Npgsql;
using EnterprisePKI.Shared.Models;
using System.Data;

namespace EnterprisePKI.Portal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        public async Task<IActionResult> GetAll()
        {
            using var db = CreateConnection();
            var agents = await db.QueryAsync<Agent>("SELECT * FROM Endpoints ORDER BY LastHeartbeat DESC");
            return Ok(agents);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterprisePKI.Portal.Controllers;

[ApiController]
[Route("api/security")]
[Authorize]
[Produces("application/json")]
public class SecurityProbeController : ControllerBase
{
    [HttpGet("probe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Probe()
    {
        return Ok(new { Status = "Authorized" });
    }
}
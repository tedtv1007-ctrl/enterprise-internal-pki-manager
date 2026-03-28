using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterprisePKI.Portal.Controllers;

[ApiController]
[Route("api/security")]
[Authorize]
public class SecurityProbeController : ControllerBase
{
    [HttpGet("probe")]
    public IActionResult Probe()
    {
        return Ok(new { Status = "Authorized" });
    }
}
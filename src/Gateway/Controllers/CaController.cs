using Microsoft.AspNetCore.Mvc;
using EnterprisePKI.Shared.Interfaces;
using EnterprisePKI.Shared.Models;

namespace EnterprisePKI.Gateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CaController : ControllerBase
    {
        private readonly ICertificateAuthority _caService;

        public CaController(ICertificateAuthority caService)
        {
            _caService = caService;
        }

        [HttpPost("issue")]
        public async Task<IActionResult> Issue([FromBody] IssueRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Csr) || string.IsNullOrWhiteSpace(request.TemplateName))
                return BadRequest(new ApiError("ValidationError", "CSR and TemplateName are required"));

            try
            {
                var cert = await _caService.IssueCertificateAsync(request.Csr, request.TemplateName);
                return Ok(cert);
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiError("InternalError", "An internal error occurred while processing the certificate request."));
            }
        }

        public class IssueRequest
        {
            public string Csr { get; set; } = string.Empty;
            public string TemplateName { get; set; } = string.Empty;
        }
    }
}

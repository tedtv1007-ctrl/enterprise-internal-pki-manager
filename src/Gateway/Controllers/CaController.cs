using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EnterprisePKI.Shared.Interfaces;
using EnterprisePKI.Shared.Models;

namespace EnterprisePKI.Gateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "GatewayIssuePolicy")]
    public class CaController : ControllerBase
    {
        private readonly ICertificateAuthority _caService;
        private readonly IGatewayIssueRequestThrottle _throttle;

        public CaController(ICertificateAuthority caService, IGatewayIssueRequestThrottle throttle)
        {
            _caService = caService;
            _throttle = throttle;
        }

        [HttpPost("issue")]
        public async Task<IActionResult> Issue([FromBody] IssueRequest request)
        {
            var headers = HttpContext?.Request?.Headers;
            var clientId = headers is not null && headers.TryGetValue("X-Client-Id", out var values)
                ? values.ToString()
                : string.Empty;
            var subject = User?.FindFirst("sub")?.Value
                ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? "gateway-authenticated";
            var partition = string.IsNullOrWhiteSpace(clientId) ? subject : $"{subject}:{clientId}";

            if (!_throttle.TryAcquire(partition))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new ApiError("RateLimited", "Too many certificate issuance requests."));
            }

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

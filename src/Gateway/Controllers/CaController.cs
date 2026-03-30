using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EnterprisePKI.Shared.Interfaces;
using EnterprisePKI.Shared.Models;

namespace EnterprisePKI.Gateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "GatewayIssuePolicy")]
    [Produces("application/json")]
    public class CaController : ControllerBase
    {
        private readonly ICertificateAuthority _caService;
        private readonly IGatewayIssueRequestThrottle _throttle;
        private readonly ILogger<CaController> _logger;

        public CaController(ICertificateAuthority caService, IGatewayIssueRequestThrottle throttle, ILogger<CaController> logger)
        {
            _caService = caService;
            _throttle = throttle;
            _logger = logger;
        }

        [HttpPost("issue")]
        [ProducesResponseType(typeof(Certificate), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
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

                var principal = User?.FindFirst("sub")?.Value
                    ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? "unknown";
                _logger.LogInformation("PKI_AUDIT: Certificate issued via Gateway — Template={Template}, CN={CommonName}, by {Principal}",
                    request.TemplateName, cert?.CommonName, principal);

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

        [HttpPost("revoke")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Revoke([FromBody] RevokeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SerialNumber))
                return BadRequest(new ApiError("ValidationError", "SerialNumber is required"));

            try
            {
                var revoked = await _caService.RevokeCertificateAsync(request.SerialNumber, request.Reason);
                if (!revoked)
                    return NotFound(new ApiError("NotFound", $"Certificate {request.SerialNumber} not found or already revoked"));

                var principal = User?.FindFirst("sub")?.Value
                    ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? "unknown";
                _logger.LogWarning("PKI_AUDIT: Certificate REVOKED — SerialNumber={SerialNumber}, Reason={Reason}, by {Principal}",
                    request.SerialNumber, request.Reason, principal);

                return Ok(new { SerialNumber = request.SerialNumber, Status = "Revoked" });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiError("InternalError", "An internal error occurred while revoking the certificate."));
            }
        }

        public class RevokeRequest
        {
            public string SerialNumber { get; set; } = string.Empty;
            public int Reason { get; set; }
        }
    }
}

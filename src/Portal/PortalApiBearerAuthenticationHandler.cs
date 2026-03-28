using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace EnterprisePKI.Portal;

public sealed class PortalApiBearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string BearerPrefix = "Bearer ";
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public PortalApiBearerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration,
        IWebHostEnvironment environment)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
        _environment = environment;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var bypassInTesting = _environment.IsEnvironment("Testing")
            && _configuration.GetValue("Portal:BypassAuthInTesting", false);

        if (bypassInTesting)
        {
            var testClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "integration-test-client"),
                new Claim("scope", "portal.api")
            };

            var testIdentity = new ClaimsIdentity(testClaims, Scheme.Name);
            var testPrincipal = new ClaimsPrincipal(testIdentity);
            var testTicket = new AuthenticationTicket(testPrincipal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(testTicket));
        }

        var configuredToken = _configuration["Portal:ApiAuthToken"];
        if (string.IsNullOrWhiteSpace(configuredToken))
        {
            return Task.FromResult(AuthenticateResult.Fail("Portal API auth token is not configured."));
        }

        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeader = authHeaderValues.ToString();
        if (!authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = authHeader[BearerPrefix.Length..].Trim();
        if (!string.Equals(token, configuredToken, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid portal API bearer token."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "portal-ui-service"),
            new Claim("scope", "portal.api")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
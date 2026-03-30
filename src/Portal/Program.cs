using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using EnterprisePKI.Portal;
using EnterprisePKI.Portal.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using EnterprisePKI.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddAuthentication("PortalApiBearer")
    .AddScheme<AuthenticationSchemeOptions, PortalApiBearerAuthenticationHandler>(
        "PortalApiBearer",
        _ => { });

builder.Services.AddAuthorization();
builder.Services.AddDataProtection();
builder.Services.AddSingleton<IDataProtectorFacade>(sp =>
{
    var provider = sp.GetRequiredService<IDataProtectionProvider>();
    return new DataProtectorFacade(provider.CreateProtector("deployment-secrets"));
});

// CORS for Blazor UI
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5261", "https://localhost:7261" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Rate limiting for Portal API endpoints
var portalPermitLimit = builder.Configuration.GetValue("Portal:RateLimiting:PermitLimit", 60);
if (portalPermitLimit < 1 || portalPermitLimit > 1000) portalPermitLimit = 60;
var portalWindowSeconds = builder.Configuration.GetValue("Portal:RateLimiting:WindowSeconds", 60);
if (portalWindowSeconds < 1 || portalWindowSeconds > 3600) portalWindowSeconds = 60;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("PortalApi", httpContext =>
    {
        var identity = httpContext.User.Identity;
        var partition = identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "portal-authenticated"
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partition,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = portalPermitLimit,
                Window = TimeSpan.FromSeconds(portalWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

// Health checks
builder.Services.AddHealthChecks();

string gatewayUrl = builder.Configuration["Gateway:Url"] ?? "http://localhost:5001";
builder.Services.AddHttpClient<EnterprisePKI.Portal.Services.GatewayService>(client => {
    client.BaseAddress = new Uri(gatewayUrl);

    var gatewayToken = builder.Configuration["Gateway:ServiceAuthToken"];
    if (!string.IsNullOrWhiteSpace(gatewayToken))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", gatewayToken);
    }
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Global exception handler — returns consistent ApiError, never leaks internals
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature is not null)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("GlobalExceptionHandler");
            logger.LogError(exceptionFeature.Error, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }

        await context.Response.WriteAsJsonAsync(
            new ApiError("InternalServerError", "An unexpected error occurred."));
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "0";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; frame-ancestors 'none'";
    await next();
});

// Correlation ID middleware
app.Use(async (context, next) =>
{
    const string correlationHeader = "X-Correlation-ID";
    if (!context.Request.Headers.TryGetValue(correlationHeader, out var correlationId)
        || string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString();
    }

    context.Items["CorrelationId"] = correlationId.ToString();
    context.Response.Headers[correlationHeader] = correlationId.ToString();

    using (app.Logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId.ToString()!
    }))
    {
        await next();
    }
});

app.UseCors("AllowUI");

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers().RequireRateLimiting("PortalApi");

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

// Required for WebApplicationFactory<Program> in Integration Tests
public partial class Program { }

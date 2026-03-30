using EnterprisePKI.Gateway.Services;
using EnterprisePKI.Shared.Interfaces;
using EnterprisePKI.Shared.Models;
using EnterprisePKI.Gateway;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient<ICertificateAuthority, AdcsGatewayService>();

builder.Services.AddAuthentication("GatewayServiceBearer")
    .AddScheme<AuthenticationSchemeOptions, GatewayServiceBearerAuthenticationHandler>(
        "GatewayServiceBearer",
        _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GatewayIssuePolicy", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "gateway.issue");
    });
});

var permitLimit = builder.Configuration.GetValue("Gateway:RateLimiting:PermitLimit", 30);
if (permitLimit < 1 || permitLimit > 1000) permitLimit = 30;
var windowSeconds = builder.Configuration.GetValue("Gateway:RateLimiting:WindowSeconds", 60);
if (windowSeconds < 1 || windowSeconds > 3600) windowSeconds = 60;

builder.Services.AddSingleton<IGatewayIssueRequestThrottle>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var permits = config.GetValue("Gateway:RateLimiting:PermitLimit", permitLimit);
    var seconds = config.GetValue("Gateway:RateLimiting:WindowSeconds", windowSeconds);
    return new GatewayIssueRequestThrottle(permits, TimeSpan.FromSeconds(seconds));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("GatewayIssue", httpContext =>
    {
        var clientId = httpContext.Request.Headers.TryGetValue("X-Client-Id", out var clientIds)
            ? clientIds.ToString()
            : string.Empty;

        var identity = httpContext.User.Identity;
        var principalPartition = identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "gateway-authenticated"
            : "gateway-unauthenticated";

        var partition = string.IsNullOrWhiteSpace(clientId)
            ? principalPartition
            : $"{principalPartition}:{clientId}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partition,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Global exception handler
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

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers().RequireRateLimiting("GatewayIssue");

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program { }

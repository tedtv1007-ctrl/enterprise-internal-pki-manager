using EnterprisePKI.Gateway.Services;
using EnterprisePKI.Shared.Interfaces;
using EnterprisePKI.Gateway;
using Microsoft.AspNetCore.Authentication;
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
var windowSeconds = builder.Configuration.GetValue("Gateway:RateLimiting:WindowSeconds", 60);

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers().RequireRateLimiting("GatewayIssue");

app.Run();

public partial class Program { }

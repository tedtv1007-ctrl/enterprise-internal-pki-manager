using System.Net.Http.Headers;
using EnterprisePKI.Portal;
using EnterprisePKI.Portal.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowUI");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Required for WebApplicationFactory<Program> in Integration Tests
public partial class Program { }

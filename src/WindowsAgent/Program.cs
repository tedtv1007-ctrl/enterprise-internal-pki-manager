using Microsoft.AspNetCore.Mvc;
using EnterprisePKI.Shared.Models;
using System.Runtime.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/adcs/submit", async ([FromBody] SubmitRequest request) =>
{
    // This part is Windows-only as it will use COM
    if (!OperatingSystem.IsWindows())
    {
        return Results.BadRequest("This proxy must run on Windows to access ADCS via DCOM.");
    }

    return await Task.Run(() => {
        try {
            // Simulated COM call logic
            // var certRequest = new CERTCLILib.CCertRequest();
            // int disposition = certRequest.Submit(CR_IN_BASE64, request.Csr, null, request.CaConfig);
            
            Console.WriteLine($"[ADCS Proxy] Submitting CSR for {request.Template}");
            
            return Results.Ok(new { 
                SerialNumber = "PROXIED-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                CertificateBase64 = "MOCK_CERT_DATA_FROM_ADCS"
            });
        }
        catch (Exception ex) {
            return Results.Problem(ex.Message);
        }
    });
});

app.Run();

public record SubmitRequest(string Csr, string Template, string CaConfig);

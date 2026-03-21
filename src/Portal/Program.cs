var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// CORS for Blazor UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", policy =>
        policy.WithOrigins("http://localhost:5261", "https://localhost:7261")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

string gatewayUrl = builder.Configuration["Gateway:Url"] ?? "http://localhost:5001";
builder.Services.AddHttpClient<EnterprisePKI.Portal.Services.GatewayService>(client => {
    client.BaseAddress = new Uri(gatewayUrl);
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

app.UseAuthorization();

app.MapControllers();

app.Run();

// Required for WebApplicationFactory<Program> in Integration Tests
public partial class Program { }

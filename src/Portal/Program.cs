var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

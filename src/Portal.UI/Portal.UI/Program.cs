using Portal.UI.Client.Pages;
using Portal.UI.Components;
using Portal.UI.Client.Services;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiBaseUrl = configuration["PortalApi:BaseUrl"] ?? "http://localhost:5069";

    var client = new HttpClient
    {
        BaseAddress = new Uri(apiBaseUrl)
    };

    var portalApiToken = configuration["PortalApi:AuthToken"];
    if (!string.IsNullOrWhiteSpace(portalApiToken))
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", portalApiToken);
    }

    return client;
});
builder.Services.AddScoped<PkiApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Portal.UI.Client._Imports).Assembly);

app.Run();

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using Portal.UI.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddRadzenComponents();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<PkiApiClient>();

await builder.Build().RunAsync();

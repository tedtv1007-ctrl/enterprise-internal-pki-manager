using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Portal.UI.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<PkiApiClient>();

await builder.Build().RunAsync();

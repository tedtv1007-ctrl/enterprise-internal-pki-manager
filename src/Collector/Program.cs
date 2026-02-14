using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EnterprisePKI.Collector.Services;
using System.Net.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using EnterprisePKI.Shared.Models;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
string portalUrl = builder.Configuration["Portal:Url"] ?? "http://localhost:5000";

// Services
builder.Services.AddHttpClient<ReportingService>((client) => {
    client.BaseAddress = new Uri(portalUrl);
});

builder.Services.AddTransient<CertificateRequestService>();
builder.Services.AddSingleton<IDiscoveryService, WindowsDiscoveryService>();
builder.Services.AddSingleton<IDeploymentService, WindowsDeploymentService>();
builder.Services.AddHostedService<CollectorWorker>();

var host = builder.Build();
host.Run();

public class CollectorWorker : BackgroundService
{
    private readonly ILogger<CollectorWorker> _logger;
    private readonly IDiscoveryService _discoveryService;
    private readonly IDeploymentService _deploymentService;
    private readonly IServiceProvider _serviceProvider;

    public CollectorWorker(
        ILogger<CollectorWorker> logger, 
        IDiscoveryService discoveryService,
        IDeploymentService deploymentService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _discoveryService = discoveryService;
        _deploymentService = deploymentService;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Collector Agent started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Running agent cycle...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var reportingService = scope.ServiceProvider.GetRequiredService<ReportingService>();

                // 1. Discovery
                var discoveredCerts = await _discoveryService.DiscoverCertificatesAsync();
                var report = new DiscoveryReport
                {
                    Hostname = Environment.MachineName,
                    Certificates = discoveredCerts
                };
                await reportingService.ReportDiscoveryAsync(report);

                // 2. Deployment Jobs
                var jobs = await reportingService.GetPendingJobsAsync(Environment.MachineName);
                foreach (var job in jobs)
                {
                    _logger.LogInformation("Processing deployment job {JobId} for cert {CertId}", job.Id, job.CertificateId);
                    
                    var success = await _deploymentService.InstallCertificateAsync(job);
                    
                    if (success)
                    {
                        await reportingService.UpdateJobStatusAsync(job.Id, "Completed");
                    }
                    else
                    {
                        await reportingService.UpdateJobStatusAsync(job.Id, "Failed", "Installation failed in deployment service");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during agent cycle.");
            }

            // Run cycle every 1 hour (configurable)
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}

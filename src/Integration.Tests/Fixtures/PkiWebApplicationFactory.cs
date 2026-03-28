using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Npgsql;
using Dapper;

namespace Integration.Tests.Fixtures;

public class PkiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("pki_test")
        .WithUsername("test")
        .WithPassword("test_password")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", ConnectionString },
                { "Gateway:Url", "http://localhost:59999" }, // Non-existent gateway for isolated tests
                { "Portal:ApiAuthToken", "integration-tests-token" },
                { "Portal:BypassAuthInTesting", "true" }
            });
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await InitializeDatabaseAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        var schemaPath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "Portal", "Schema", "init.sql");
        
        if (!File.Exists(schemaPath))
        {
            // Try alternative path
            schemaPath = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Portal", "Schema", "init.sql"));
        }

        if (File.Exists(schemaPath))
        {
            var sql = await File.ReadAllTextAsync(schemaPath);
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync(sql);
        }
        else
        {
            // Fallback: create minimal schema
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Certificates (
                    Id UUID PRIMARY KEY,
                    CommonName VARCHAR(255) NOT NULL,
                    SerialNumber VARCHAR(128) NOT NULL,
                    Thumbprint VARCHAR(128) NOT NULL,
                    IssuerDN TEXT NOT NULL,
                    NotBefore TIMESTAMP WITH TIME ZONE NOT NULL,
                    NotAfter TIMESTAMP WITH TIME ZONE NOT NULL,
                    Algorithm VARCHAR(64) NOT NULL,
                    KeySize INT NOT NULL,
                    IsPQC BOOLEAN DEFAULT FALSE,
                    RawData BYTEA,
                    Status VARCHAR(32) DEFAULT 'Active',
                    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                );
                CREATE TABLE IF NOT EXISTS Endpoints (
                    Id UUID PRIMARY KEY,
                    Hostname VARCHAR(255) NOT NULL,
                    IPAddress VARCHAR(64),
                    Type VARCHAR(64) NOT NULL,
                    LastHeartbeat TIMESTAMP WITH TIME ZONE
                );
                CREATE TABLE IF NOT EXISTS CertificateDeployments (
                    CertificateId UUID REFERENCES Certificates(Id),
                    EndpointId UUID REFERENCES Endpoints(Id),
                    DeployedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                    LastSeen TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY (CertificateId, EndpointId)
                );
                CREATE TABLE IF NOT EXISTS CertificateRequests (
                    Id UUID PRIMARY KEY,
                    Requester VARCHAR(255) NOT NULL,
                    CSR TEXT NOT NULL,
                    TemplateName VARCHAR(128) NOT NULL,
                    Status VARCHAR(32) DEFAULT 'Pending',
                    CertificateId UUID REFERENCES Certificates(Id),
                    RequestedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                );
                CREATE TABLE IF NOT EXISTS DeploymentJobs (
                    Id UUID PRIMARY KEY,
                    CertificateId UUID REFERENCES Certificates(Id),
                    TargetHostname VARCHAR(255) NOT NULL,
                    StoreLocation VARCHAR(255) NOT NULL,
                    Status VARCHAR(32) DEFAULT 'Pending',
                    ErrorMessage TEXT,
                    PfxData TEXT,
                    PfxPassword TEXT,
                    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                    CompletedAt TIMESTAMP WITH TIME ZONE
                );
            ");
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}

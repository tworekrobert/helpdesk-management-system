using Helpdesk.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Helpdesk.IntegrationTests;

public sealed class HelpdeskApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("helpdesk_tests")
        .WithUsername("helpdesk")
        .WithPassword("helpdesk")
        .Build();

    public HelpdeskApiFactory()
    {
        // Program.cs reads the key before ConfigureWebHost runs, and the Testing
        // environment doesn't load appsettings.Development.json.
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "helpdesk-integration-tests-signing-key-32b");
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<HelpdeskDbContext>>();
            services.AddDbContext<HelpdeskDbContext>(options =>
                options.UseNpgsql(_database.GetConnectionString()));
        });
    }
}

using Helpdesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Helpdesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=helpdesk;Username=helpdesk;Password=helpdesk";

        services.AddDbContext<HelpdeskDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}

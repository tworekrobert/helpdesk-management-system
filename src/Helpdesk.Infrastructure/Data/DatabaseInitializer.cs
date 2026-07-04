using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Infrastructure.Data;

public sealed class DatabaseInitializer(HelpdeskDbContext db)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);
        await DatabaseSeeder.SeedAsync(db, cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vaultly.Core.Persistence;

public static class DatabaseMigration
{
    /// <summary>
    /// Applies pending EF Core migrations. Meant for single-instance setups (local, Docker Compose); with several
    /// instances run the migrations as a separate deployment step instead.
    /// </summary>
    public static async Task MigrateDatabaseAsync<TDbContext>(this IServiceProvider services, CancellationToken cancellationToken = default)
        where TDbContext : DbContext
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TDbContext>().Database.MigrateAsync(cancellationToken);
    }
}

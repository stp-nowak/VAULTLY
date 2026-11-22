using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vaultly.Identity.Infrastructure.Persistence;

public static class IdentityDatabaseMigration
{
    /// <summary>
    /// Applies pending EF Core migrations. Meant for single-instance setups (local, Docker Compose); with several
    /// instances run the migrations as a separate deployment step instead.
    /// </summary>
    public static async Task MigrateIdentityDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync(cancellationToken);
    }
}

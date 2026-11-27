using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Vaultly.Core.Persistence;

public static class PostgresOptions
{
    /// <summary>
    /// Connects a service's DbContext to PostgreSQL. GSS encryption is switched off: services sign in with a password,
    /// and Npgsql's default attempt to negotiate it needs the Kerberos library the runtime images do not ship.
    /// </summary>
    public static DbContextOptionsBuilder UsePostgres(this DbContextOptionsBuilder options, string? connectionString)
        => options.UseNpgsql(new NpgsqlConnectionStringBuilder(connectionString)
        {
            GssEncryptionMode = GssEncryptionMode.Disable
        }.ConnectionString);
}

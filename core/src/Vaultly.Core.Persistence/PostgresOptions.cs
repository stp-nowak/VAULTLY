using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Vaultly.Core.Persistence;

public static class PostgresOptions
{
    /// <summary>Connects a service's DbContext to PostgreSQL through <see cref="ConnectionString"/>.</summary>
    public static DbContextOptionsBuilder UsePostgres(this DbContextOptionsBuilder options, string? connectionString)
        => options.UseNpgsql(ConnectionString(connectionString));

    /// <summary>
    /// The connection string every Postgres client of a service uses. GSS encryption is switched off: services sign in
    /// with a password, and Npgsql's default attempt to negotiate it needs the Kerberos library the runtime images do
    /// not ship.
    /// </summary>
    public static string ConnectionString(string? connectionString)
        => new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable }.ConnectionString;
}

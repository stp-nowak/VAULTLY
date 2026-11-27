using Microsoft.EntityFrameworkCore;

using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;

using NUnit.Framework;

namespace Vaultly.Core.Persistence.Tests;

[TestFixture]
public sealed class PostgresOptionsTests
{
    [Test]
    public void UsePostgres_KeepsTheConnectionString_AndNeverTriesGssEncryption()
    {
        var options = new DbContextOptionsBuilder()
            .UsePostgres("Host=db;Database=vaultly;Username=app;Password=secret;Gss Encryption Mode=Prefer")
            .Options;

        var connection = new NpgsqlConnectionStringBuilder(options.FindExtension<NpgsqlOptionsExtension>()!.ConnectionString);

        Assert.Multiple(() =>
        {
            Assert.That(connection.Host, Is.EqualTo("db"));
            Assert.That(connection.Database, Is.EqualTo("vaultly"));
            Assert.That(connection.GssEncryptionMode, Is.EqualTo(GssEncryptionMode.Disable));
        });
    }
}

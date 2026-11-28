using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Vaultly.Transactions.Infrastructure.Persistence;

namespace Vaultly.Transactions.Application.Tests;

[TestFixture]
public sealed class TransactionsDbContextTests
{
    [Test]
    public void Migrations_WhenComparedWithTheModel_LeaveNoPendingChanges()
    {
        using var dbContext = new TransactionsDbContextDesignTimeFactory().CreateDbContext([]);

        Assert.That(
            dbContext.Database.HasPendingModelChanges(),
            Is.False,
            "The model changed without a migration: run `dotnet ef migrations add <Name>` (see transactions/AGENTS.md).");
    }
}

using NUnit.Framework;

using Microsoft.EntityFrameworkCore;

using Vaultly.Accounts.Infrastructure.Persistence;

namespace Vaultly.Accounts.Application.Tests;

[TestFixture]
public sealed class AccountsDbContextTests
{
    [Test]
    public void Migrations_WhenComparedWithTheModel_LeaveNoPendingChanges()
    {
        using var dbContext = new AccountsDbContextDesignTimeFactory().CreateDbContext([]);

        Assert.That(
            dbContext.Database.HasPendingModelChanges(),
            Is.False,
            "The model changed without a migration: run `dotnet ef migrations add <Name>` (see accounts/AGENTS.md).");
    }
}

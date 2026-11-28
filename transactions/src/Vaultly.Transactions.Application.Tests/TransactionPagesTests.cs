using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Testcontainers.PostgreSql;

using Vaultly.Core.Amounts;
using Vaultly.Core.Persistence;
using Vaultly.Transactions.Application.Features.Transactions.GetTransactions;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;
using Vaultly.Transactions.Infrastructure.Persistence;
using Vaultly.Transactions.Infrastructure.Transactions;

namespace Vaultly.Transactions.Application.Tests;

/// <summary>Pages on a real Postgres (Testcontainers, needs Docker): only it numbers transactions in recording order.</summary>
[TestFixture]
public sealed class TransactionPagesTests
{
    private static readonly OwnerId Owner = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly DateOnly Day = new(2026, 11, 20);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:19-alpine").Build();

    [OneTimeSetUp]
    public async Task StartDatabase()
    {
        await _postgres.StartAsync();
        await using var dbContext = DbContext();
        await dbContext.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public Task StopDatabase() => _postgres.DisposeAsync().AsTask();

    [Test]
    public async Task Pages_FollowEachOther_WithoutGapsOrRepeats_AlsoWithinADay()
    {
        var wallet = Account.Opened(new AccountId(Guid.NewGuid()), Owner, new Currency("PLN"), 1);
        var recorded = new List<Guid>();
        foreach (var day in new[] { Day, Day.AddDays(1), Day.AddDays(1), Day.AddDays(1), Day })
        {
            await using var dbContext = DbContext();
            var transaction = Transaction.Record(wallet, -10m, day, new TransactionDescription("Shop"), null, DateTimeOffset.UnixEpoch);
            dbContext.Transactions.Add(transaction);
            await dbContext.SaveChangesAsync();
            recorded.Add(transaction.Id.Value);
        }

        var seen = new List<Guid>();
        TransactionsCursor? cursor = null;
        var pages = 0;
        do
        {
            await using var dbContext = DbContext();
            var page = await new TransactionReadService(dbContext)
                .GetTransactionsAsync(Owner, wallet.Id, null, null, cursor, 2, CancellationToken.None);
            seen.AddRange(page.Items.Select(x => x.Id));
            cursor = page.Next;
            pages++;
        }
        while (cursor is not null);

        Assert.Multiple(() =>
        {
            Assert.That(pages, Is.EqualTo(3));
            Assert.That(seen, Is.EqualTo(new[] { recorded[3], recorded[2], recorded[1], recorded[4], recorded[0] }));
        });
    }

    private TransactionsDbContext DbContext()
    {
        var options = new DbContextOptionsBuilder<TransactionsDbContext>();
        options.UsePostgres(_postgres.GetConnectionString());
        return new TransactionsDbContext(options.Options);
    }
}

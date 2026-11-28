using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Vaultly.Accounts.Application.Features.Accounts.GetBalanceHistory;
using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Accounts.Domain.Postings;
using Vaultly.Accounts.Infrastructure.Persistence;
using Vaultly.Accounts.Infrastructure.Postings;
using Vaultly.Core.Amounts;

namespace Vaultly.Accounts.Application.Tests;

[TestFixture]
public sealed class BalanceHistoryReadServiceTests
{
    private static readonly OwnerId Owner = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly DateOnly Day = new(2026, 11, 20);

    private AccountsDbContext _dbContext = null!;

    [SetUp]
    public void SetUp()
        => _dbContext = new AccountsDbContext(new DbContextOptionsBuilder<AccountsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task Movements_SumTheAccountsLivePostingsPerDay_OldestFirst()
    {
        var account = AddAccount(Owner);
        var other = AddAccount(Owner);
        AddPosting(account, Day.AddDays(1), 20m);
        AddPosting(account, Day, -50m);
        AddPosting(account, Day, -5m);
        AddPosting(account, Day, -1000m, deleted: true);
        AddPosting(other, Day, -7m);
        await _dbContext.SaveChangesAsync();

        var movements = await new BalanceHistoryReadService(_dbContext).GetMovementsAsync(account.Id, Owner, CancellationToken.None);

        Assert.That(movements, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(movements!.OpeningBalance, Is.EqualTo(100m));
            Assert.That(movements.DailyTotals, Is.EqualTo(new[] { new DailyTotal(Day, -55m), new DailyTotal(Day.AddDays(1), 20m) }));
        });
    }

    [Test]
    public async Task Movements_OfAnotherOwnersAccount_AreNull()
    {
        var account = AddAccount(new OwnerId(Guid.NewGuid()));
        await _dbContext.SaveChangesAsync();

        var movements = await new BalanceHistoryReadService(_dbContext).GetMovementsAsync(account.Id, Owner, CancellationToken.None);

        Assert.That(movements, Is.Null);
    }

    private Account AddAccount(OwnerId owner)
    {
        var account = Account.Open(owner, new AccountName("Wallet"), AccountType.Cash, new Money(100m, new Currency("PLN")), DateTimeOffset.UnixEpoch);
        _dbContext.Accounts.Add(account);
        return account;
    }

    private void AddPosting(Account account, DateOnly bookedOn, decimal amount, bool deleted = false)
    {
        var posting = Posting.Start(new TransactionId(Guid.NewGuid()), account.Id);
        posting.Book(amount, bookedOn, Guid.NewGuid());
        if (deleted)
        {
            posting.Delete(Guid.NewGuid());
        }

        _dbContext.Postings.Add(posting);
    }
}

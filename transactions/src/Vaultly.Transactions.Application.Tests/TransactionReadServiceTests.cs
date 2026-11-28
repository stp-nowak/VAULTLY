using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Vaultly.Core.Amounts;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;
using Vaultly.Transactions.Infrastructure.Persistence;
using Vaultly.Transactions.Infrastructure.Transactions;

namespace Vaultly.Transactions.Application.Tests;

[TestFixture]
public sealed class TransactionReadServiceTests
{
    private static readonly OwnerId Owner = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly DateOnly Day = new(2026, 11, 20);

    private TransactionsDbContext _dbContext = null!;
    private TransactionReadService _readService = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = new TransactionsDbContext(new DbContextOptionsBuilder<TransactionsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        _readService = new TransactionReadService(_dbContext);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task List_HasTheOwnersLiveTransactions_NewestDayFirst_FilteredByAccountAndDays()
    {
        var wallet = Account.Opened(new AccountId(Guid.NewGuid()), Owner, new Currency("PLN"), 1);
        var card = Account.Opened(new AccountId(Guid.NewGuid()), Owner, new Currency("PLN"), 1);
        var foreign = Account.Opened(new AccountId(Guid.NewGuid()), new OwnerId(Guid.NewGuid()), new Currency("PLN"), 1);
        var older = Add(wallet, Day);
        var newer = Add(wallet, Day.AddDays(2));
        var deleted = Add(wallet, Day.AddDays(1));
        deleted.ConfirmPosting(deleted.PendingStepId!.Value);
        deleted.Delete();
        deleted.ConfirmPosting(deleted.PendingStepId!.Value);
        Add(card, Day);
        Add(foreign, Day);
        await _dbContext.SaveChangesAsync();

        var all = await _readService.GetTransactionsAsync(Owner, wallet.Id, null, null, null, 10, CancellationToken.None);
        var fromSecondDay = await _readService.GetTransactionsAsync(Owner, null, Day.AddDays(1), null, null, 10, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(all.Items.Select(x => x.Id), Is.EqualTo(new[] { newer.Id.Value, older.Id.Value }));
            Assert.That(all.Next, Is.Null);
            Assert.That(fromSecondDay.Items.Select(x => x.Id), Is.EqualTo(new[] { newer.Id.Value }));
        });
    }

    [Test]
    public async Task One_OfAnotherOwner_IsNull()
    {
        var foreign = Account.Opened(new AccountId(Guid.NewGuid()), new OwnerId(Guid.NewGuid()), new Currency("PLN"), 1);
        var transaction = Add(foreign, Day);
        await _dbContext.SaveChangesAsync();

        Assert.That(await _readService.GetTransactionAsync(transaction.Id, Owner, CancellationToken.None), Is.Null);
    }

    private Transaction Add(Account account, DateOnly bookedOn)
    {
        var transaction = Transaction.Record(account, -10m, bookedOn, new TransactionDescription("Shop"), null, DateTimeOffset.UnixEpoch);
        _dbContext.Transactions.Add(transaction);
        return transaction;
    }
}

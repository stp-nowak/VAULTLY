using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Vaultly.Accounts.Application.Features.NetWorth.GetNetWorth;
using Vaultly.Accounts.Domain.Aggregates;
using Vaultly.Accounts.Domain.Enums;
using Vaultly.Accounts.Domain.ValueObjects;
using Vaultly.Accounts.Infrastructure.Persistence;
using Vaultly.Accounts.Infrastructure.Persistence.ReadServices;

namespace Vaultly.Accounts.Application.Tests;

[TestFixture]
public sealed class AccountReadServiceTests
{
    private static readonly OwnerId Owner = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly OwnerId OtherOwner = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    private AccountsDbContext _dbContext = null!;
    private AccountReadService _readService = null!;

    [SetUp]
    public void SetUp()
    {
        _dbContext = new AccountsDbContext(new DbContextOptionsBuilder<AccountsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        _readService = new AccountReadService(_dbContext);
    }

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

    [Test]
    public async Task GetAccountsAsync_ListsOnlyTheOwnersOpenAccounts_OldestFirst()
    {
        var second = await AddAsync(Owner, "Second", openedDaysAgo: 1);
        var first = await AddAsync(Owner, "First", openedDaysAgo: 2);
        await AddAsync(Owner, "Closed", closed: true);
        await AddAsync(OtherOwner, "Someone else");

        var accounts = await _readService.GetAccountsAsync(Owner, includeClosed: false, CancellationToken.None);

        Assert.That(accounts.Select(x => x.Id), Is.EqualTo(new[] { first.Id.Value, second.Id.Value }));
    }

    [Test]
    public async Task GetAccountsAsync_WithClosed_IncludesThem()
    {
        await AddAsync(Owner, "Open");
        await AddAsync(Owner, "Closed", closed: true);

        var accounts = await _readService.GetAccountsAsync(Owner, includeClosed: true, CancellationToken.None);

        Assert.That(accounts.Select(x => x.Name), Is.EquivalentTo(new[] { "Open", "Closed" }));
    }

    [Test]
    public async Task GetAccountAsync_OfAnotherOwner_IsNull()
    {
        var account = await AddAsync(OtherOwner, "Someone else");

        Assert.That(await _readService.GetAccountAsync(account.Id, Owner, CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task GetNetWorthAsync_SumsAssetsAndDebtsPerCurrency_FromOpenAccountsOnly()
    {
        await AddAsync(Owner, "Checking", balance: 1000m);
        await AddAsync(Owner, "Savings", balance: 500.25m);
        await AddAsync(Owner, "Card", balance: -300m, type: AccountType.CreditCard);
        await AddAsync(Owner, "Euro", balance: 40m, currency: "EUR");
        await AddAsync(Owner, "Closed", balance: 9999m, closed: true);
        await AddAsync(OtherOwner, "Someone else", balance: 7777m);

        var netWorth = await _readService.GetNetWorthAsync(Owner, CancellationToken.None);

        Assert.That(netWorth, Is.EqualTo(new[]
        {
            new CurrencyNetWorthDto("EUR", 40m, 0m, 40m),
            new CurrencyNetWorthDto("PLN", 1500.25m, 300m, 1200.25m)
        }));
    }

    private async Task<Account> AddAsync(
        OwnerId owner,
        string name,
        decimal balance = 0m,
        string currency = "PLN",
        AccountType type = AccountType.Checking,
        bool closed = false,
        int openedDaysAgo = 0)
    {
        var account = Account.Open(owner, new AccountName(name), type, new Money(balance, new Currency(currency)), Now.AddDays(-openedDaysAgo));
        if (closed)
        {
            account.Close(Now);
        }

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        return account;
    }
}

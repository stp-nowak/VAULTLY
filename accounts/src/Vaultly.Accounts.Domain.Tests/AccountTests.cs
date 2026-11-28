using NUnit.Framework;


using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Accounts.Domain.Accounts.Events;
using Vaultly.Core;
using Vaultly.Core.Amounts;

namespace Vaultly.Accounts.Domain.Tests;

[TestFixture]
public sealed class AccountTests
{
    private static readonly OwnerId Owner = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void Open_StartsAtTheOpeningBalance_InItsCurrency()
    {
        var account = Open(type: AccountType.CreditCard, openingBalance: -250.5m);

        Assert.Multiple(() =>
        {
            Assert.That(account.OwnerId, Is.EqualTo(Owner));
            Assert.That(account.Name.Value, Is.EqualTo("Wallet"));
            Assert.That(account.Type, Is.EqualTo(AccountType.CreditCard));
            Assert.That(account.Currency.Code, Is.EqualTo("PLN"));
            Assert.That(account.OpeningBalance, Is.EqualTo(-250.5m));
            Assert.That(account.Balance, Is.EqualTo(-250.5m));
            Assert.That(account.OpenedAt, Is.EqualTo(Now));
            Assert.That(account.IsClosed, Is.False);
        });
    }

    [Test]
    public void Open_RaisesAccountOpened()
    {
        var account = Open();

        Assert.That(account.DomainEvents.Single(), Is.TypeOf<AccountOpened>()
            .With.Property(nameof(AccountOpened.AccountId)).EqualTo(account.Id)
            .And.Property(nameof(AccountOpened.OwnerId)).EqualTo(Owner));
    }

    [Test]
    public void Open_WithUndefinedType_Throws()
        => Assert.Throws<InvalidValueException>(() => Open(type: (AccountType)42));

    [Test]
    public void Rename_ChangesTheName()
    {
        var account = Open();

        account.Rename(new AccountName("Savings pot"));

        Assert.That(account.Name.Value, Is.EqualTo("Savings pot"));
    }

    [Test]
    public void Close_RecordsWhen_AndRaisesAccountClosed()
    {
        var account = Open();
        account.ClearDomainEvents();

        account.Close(Now.AddDays(1));

        Assert.Multiple(() =>
        {
            Assert.That(account.ClosedAt, Is.EqualTo(Now.AddDays(1)));
            Assert.That(account.IsClosed, Is.True);
            Assert.That(account.DomainEvents.Single(), Is.TypeOf<AccountClosed>());
        });
    }

    [Test]
    public void ClosedAccount_CannotBeRenamedOrClosedAgain()
    {
        var account = Open();
        account.Close(Now);

        Assert.Multiple(() =>
        {
            Assert.Throws<AccountClosedException>(() => account.Rename(new AccountName("Other")));
            Assert.Throws<AccountClosedException>(() => account.Close(Now));
        });
    }

    private static Account Open(AccountType type = AccountType.Cash, decimal openingBalance = 100m)
        => Account.Open(Owner, new AccountName("Wallet"), type, new Money(openingBalance, new Currency("PLN")), Now);
}

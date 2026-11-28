using NUnit.Framework;

using Vaultly.Core.Amounts;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Domain.Tests;

[TestFixture]
public sealed class AccountTests
{
    private static readonly AccountId Id = new(Guid.NewGuid());
    private static readonly OwnerId Owner = new(Guid.NewGuid());

    [Test]
    public void Closing_ClosesTheAccount()
    {
        var account = Account.Opened(Id, Owner, new Currency("PLN"), 1);

        account.ApplyClosed(3);

        Assert.Multiple(() =>
        {
            Assert.That(account.IsClosed, Is.True);
            Assert.That(account.Version, Is.EqualTo(3));
        });
    }

    [Test]
    public void OpeningArrivingAfterTheClosing_OnlyFillsInTheCurrency()
    {
        var account = Account.Closed(Id, Owner, 3);

        account.ApplyOpened(new Currency("EUR"), 1);

        Assert.Multiple(() =>
        {
            Assert.That(account.IsClosed, Is.True);
            Assert.That(account.Version, Is.EqualTo(3));
            Assert.That(account.Currency?.Code, Is.EqualTo("EUR"));
        });
    }

    [Test]
    public void OlderClosing_ChangesNothing()
    {
        var account = Account.Opened(Id, Owner, new Currency("PLN"), 5);

        account.ApplyClosed(4);

        Assert.That(account.IsClosed, Is.False);
    }
}

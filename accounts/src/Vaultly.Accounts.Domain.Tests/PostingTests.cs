using NUnit.Framework;

using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Accounts.Domain.Postings;
using Vaultly.Core.Amounts;

namespace Vaultly.Accounts.Domain.Tests;

[TestFixture]
public sealed class PostingTests
{
    private static readonly DateOnly Day = new(2026, 11, 20);
    private static readonly Guid FirstStep = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid SecondStep = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    [Test]
    public void NewPosting_AddsNothing_UntilBooked()
    {
        var posting = Posting.Start(new TransactionId(Guid.NewGuid()), AccountId.New());

        Assert.That(posting.Effect, Is.Zero);
    }

    [Test]
    public void Book_SetsWhatItAdds_AndRemembersTheStep()
    {
        var posting = Posting.Start(new TransactionId(Guid.NewGuid()), AccountId.New());

        posting.Book(-50m, Day, FirstStep);

        Assert.Multiple(() =>
        {
            Assert.That(posting.Effect, Is.EqualTo(-50m));
            Assert.That(posting.BookedOn, Is.EqualTo(Day));
            Assert.That(posting.HasApplied(FirstStep), Is.True);
            Assert.That(posting.HasApplied(SecondStep), Is.False);
        });
    }

    [Test]
    public void Delete_LeavesNothingAdded()
    {
        var posting = Posting.Start(new TransactionId(Guid.NewGuid()), AccountId.New());
        posting.Book(-50m, Day, FirstStep);

        posting.Delete(SecondStep);

        Assert.Multiple(() =>
        {
            Assert.That(posting.Effect, Is.Zero);
            Assert.That(posting.IsDeleted, Is.True);
            Assert.That(posting.HasApplied(SecondStep), Is.True);
        });
    }

    [Test]
    public void ApplyPosting_ReplacesWhatTheTransactionAdded_AndRaisesTheVersion()
    {
        var account = Account.Open(new OwnerId(Guid.NewGuid()), new AccountName("Wallet"), AccountType.Cash,
            new Money(100m, new Currency("PLN")), DateTimeOffset.UnixEpoch);

        account.ApplyPosting(0m, -50m);
        account.ApplyPosting(-50m, -80m);

        Assert.Multiple(() =>
        {
            Assert.That(account.Balance, Is.EqualTo(20m));
            Assert.That(account.OpeningBalance, Is.EqualTo(100m));
            Assert.That(account.Version, Is.EqualTo(3));
        });
    }

    [Test]
    public void ClosedAccount_TakesNoPostings()
    {
        var account = Account.Open(new OwnerId(Guid.NewGuid()), new AccountName("Wallet"), AccountType.Cash,
            new Money(100m, new Currency("PLN")), DateTimeOffset.UnixEpoch);
        account.Close(DateTimeOffset.UnixEpoch);

        Assert.Throws<AccountClosedException>(() => account.ApplyPosting(0m, -50m));
    }
}

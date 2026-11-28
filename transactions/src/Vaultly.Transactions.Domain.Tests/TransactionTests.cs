using NUnit.Framework;

using Vaultly.Core;
using Vaultly.Core.Amounts;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;
using Vaultly.Transactions.Domain.Transactions.Exceptions;

namespace Vaultly.Transactions.Domain.Tests;

[TestFixture]
public sealed class TransactionTests
{
    private static readonly OwnerId Owner = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly AccountId AccountId = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateOnly Day = new(2026, 11, 20);
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void Record_IsPending_InTheAccountsCurrency_AndAsksToPostIt()
    {
        var transaction = Record(-50m);

        var requested = (PostingRequested)transaction.DomainEvents.Single();
        Assert.Multiple(() =>
        {
            Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Pending));
            Assert.That(transaction.PendingStep, Is.EqualTo(PostingStep.Record));
            Assert.That(transaction.OwnerId, Is.EqualTo(Owner));
            Assert.That(transaction.Currency.Code, Is.EqualTo("PLN"));
            Assert.That(requested.Step, Is.EqualTo(PostingStep.Record));
            Assert.That(requested.StepId, Is.EqualTo(transaction.PendingStepId));
            Assert.That(requested.Amount, Is.EqualTo(-50m));
            Assert.That(requested.BookedOn, Is.EqualTo(Day));
        });
    }

    [Test]
    public void Record_OnAClosedAccount_Throws()
    {
        var account = OpenAccount();
        account.ApplyClosed(2);

        Assert.Throws<AccountClosedException>(() => Transaction.Record(account, -50m, Day, new TransactionDescription("Shop"), null, Now));
    }

    [Test]
    public void Record_WithTooPreciseAmount_Throws()
        => Assert.Throws<InvalidValueException>(() => Record(-0.00001m));

    [Test]
    public void ConfirmedRecord_IsPosted()
    {
        var transaction = Posted(-50m);

        Assert.Multiple(() =>
        {
            Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Posted));
            Assert.That(transaction.PendingStep, Is.Null);
            Assert.That(transaction.PostedAmount, Is.EqualTo(-50m));
        });
    }

    [Test]
    public void DeclinedRecord_IsRejected_WithTheReason()
    {
        var transaction = Record(-50m);

        transaction.Decline(transaction.PendingStepId!.Value, DeclineReason.AccountClosed);

        Assert.Multiple(() =>
        {
            Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Rejected));
            Assert.That(transaction.DeclineReason, Is.EqualTo(DeclineReason.AccountClosed));
        });
    }

    [Test]
    public void AnswerToAnotherStep_IsIgnored()
    {
        var transaction = Record(-50m);

        transaction.ConfirmPosting(Guid.NewGuid());
        transaction.Decline(Guid.NewGuid(), DeclineReason.AccountClosed);

        Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Pending));
    }

    [Test]
    public void ChangingTheAmount_IsAPendingStep_ThatAsksForTheNewAmount()
    {
        var transaction = Posted(-50m);

        var pending = transaction.Change(-80m, Day.AddDays(1), new TransactionDescription("Shop"), null);

        var requested = (PostingRequested)transaction.DomainEvents.Single();
        Assert.Multiple(() =>
        {
            Assert.That(pending, Is.True);
            Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Pending));
            Assert.That(requested.Step, Is.EqualTo(PostingStep.Change));
            Assert.That(requested.Amount, Is.EqualTo(-80m));
            Assert.That(requested.BookedOn, Is.EqualTo(Day.AddDays(1)));
        });
    }

    [Test]
    public void ChangingOnlyTheDescription_TakesEffectAtOnce()
    {
        var transaction = Posted(-50m);

        var pending = transaction.Change(-50m, Day, new TransactionDescription("Groceries"), TransactionNote.From("weekly"));

        Assert.Multiple(() =>
        {
            Assert.That(pending, Is.False);
            Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Posted));
            Assert.That(transaction.Description.Value, Is.EqualTo("Groceries"));
            Assert.That(transaction.Note?.Value, Is.EqualTo("weekly"));
            Assert.That(transaction.DomainEvents, Is.Empty);
        });
    }

    [Test]
    public void DeclinedChange_GoesBackToThePostedAmountAndDate()
    {
        var transaction = Posted(-50m);
        transaction.Change(-80m, Day.AddDays(1), new TransactionDescription("Shop"), null);

        transaction.Decline(transaction.PendingStepId!.Value, DeclineReason.AccountClosed);

        Assert.Multiple(() =>
        {
            Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Posted));
            Assert.That(transaction.Amount, Is.EqualTo(-50m));
            Assert.That(transaction.BookedOn, Is.EqualTo(Day));
            Assert.That(transaction.DeclineReason, Is.EqualTo(DeclineReason.AccountClosed));
        });
    }

    [Test]
    public void ConfirmedChange_BecomesThePostedAmount()
    {
        var transaction = Posted(-50m);
        transaction.Change(-80m, Day, new TransactionDescription("Shop"), null);

        transaction.ConfirmPosting(transaction.PendingStepId!.Value);

        Assert.That(transaction.PostedAmount, Is.EqualTo(-80m));
    }

    [Test]
    public void Deleting_IsAPendingStep_ThatAsksToTakeEverythingOff()
    {
        var transaction = Posted(-50m);

        var pending = transaction.Delete();

        var requested = (PostingRequested)transaction.DomainEvents.Single();
        Assert.Multiple(() =>
        {
            Assert.That(pending, Is.True);
            Assert.That(requested.Step, Is.EqualTo(PostingStep.Delete));
            Assert.That(requested.Amount, Is.Zero);
        });
    }

    [Test]
    public void ConfirmedDeletion_Deletes_AndDeclinedDeletion_KeepsItPosted()
    {
        var deleted = Posted(-50m);
        deleted.Delete();
        var kept = Posted(-50m);
        kept.Delete();

        deleted.ConfirmPosting(deleted.PendingStepId!.Value);
        kept.Decline(kept.PendingStepId!.Value, DeclineReason.AccountClosed);

        Assert.Multiple(() =>
        {
            Assert.That(deleted.Status, Is.EqualTo(TransactionStatus.Deleted));
            Assert.That(kept.Status, Is.EqualTo(TransactionStatus.Posted));
        });
    }

    [Test]
    public void PendingTransaction_TakesNoOtherStep()
    {
        var transaction = Record(-50m);

        Assert.Multiple(() =>
        {
            Assert.Throws<TransactionPendingException>(() => transaction.Change(-80m, Day, new TransactionDescription("Shop"), null));
            Assert.Throws<TransactionPendingException>(() => transaction.Delete());
        });
    }

    [Test]
    public void RejectedTransaction_CannotChange_ButIsDeletedAtOnce()
    {
        var transaction = Record(-50m);
        transaction.Decline(transaction.PendingStepId!.Value, DeclineReason.AccountClosed);
        transaction.ClearDomainEvents();

        Assert.Throws<TransactionRejectedException>(() => transaction.Change(-80m, Day, new TransactionDescription("Shop"), null));
        var pending = transaction.Delete();

        Assert.Multiple(() =>
        {
            Assert.That(pending, Is.False);
            Assert.That(transaction.Status, Is.EqualTo(TransactionStatus.Deleted));
            Assert.That(transaction.DomainEvents, Is.Empty);
        });
    }

    [Test]
    public void Description_IsRequired_AndNoteBlankIsNone()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidValueException>(() => _ = new TransactionDescription(" "));
            Assert.Throws<InvalidValueException>(() => _ = new TransactionDescription(new string('a', TransactionDescription.MaxLength + 1)));
            Assert.That(TransactionNote.From("  "), Is.Null);
            Assert.Throws<InvalidValueException>(() => TransactionNote.From(new string('a', TransactionNote.MaxLength + 1)));
        });
    }

    private static Account OpenAccount() => Account.Opened(AccountId, Owner, new Currency("PLN"), 1);

    private static Transaction Record(decimal amount)
        => Transaction.Record(OpenAccount(), amount, Day, new TransactionDescription("Shop"), null, Now);

    private static Transaction Posted(decimal amount)
    {
        var transaction = Record(amount);
        transaction.ConfirmPosting(transaction.PendingStepId!.Value);
        transaction.ClearDomainEvents();
        return transaction;
    }
}

using Vaultly.Core;
using Vaultly.Core.Amounts;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions.Exceptions;

namespace Vaultly.Transactions.Domain.Transactions;

/// <summary>
/// A transaction the user entered on a manual account. Every change of its amount or date, and its deletion, is a
/// step that Accounts must apply to the account's balance: until Accounts answers the transaction is pending and takes
/// no other step. If Accounts declines, the step is undone: a new transaction is rejected, a change goes back to the
/// posted amount and date, a deletion back to posted.
/// </summary>
public sealed class Transaction : AggregateRoot
{
    private Transaction()
    {
    }

    public TransactionId Id { get; private set; }

    public OwnerId OwnerId { get; private set; }

    public AccountId AccountId { get; private set; }

    /// <summary>Signed: money spent is negative. While a change is pending, the amount asked for.</summary>
    public decimal Amount { get; private set; }

    public Currency Currency { get; private set; } = null!;

    public DateOnly BookedOn { get; private set; }

    public TransactionDescription Description { get; private set; } = null!;

    public TransactionNote? Note { get; private set; }

    public TransactionStatus Status { get; private set; }

    /// <summary>The step waiting for Accounts, while <see cref="Status"/> is pending.</summary>
    public PostingStep? PendingStep { get; private set; }

    public Guid? PendingStepId { get; private set; }

    /// <summary>The amount the account's balance reflects, restored when a change is declined.</summary>
    public decimal PostedAmount { get; private set; }

    public DateOnly PostedBookedOn { get; private set; }

    /// <summary>Why Accounts declined the last step, if it did.</summary>
    public DeclineReason? DeclineReason { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public int Version { get; private set; }

    public static Transaction Record(
        Account account,
        decimal amount,
        DateOnly bookedOn,
        TransactionDescription description,
        TransactionNote? note,
        DateTimeOffset now)
    {
        if (account.IsClosed || account.Currency is null)
        {
            throw new AccountClosedException(account.Id);
        }

        var transaction = new Transaction
        {
            Id = TransactionId.New(),
            OwnerId = account.OwnerId,
            AccountId = account.Id,
            Amount = new Money(amount, account.Currency).Amount,
            Currency = account.Currency,
            BookedOn = bookedOn,
            Description = description,
            Note = note,
            RecordedAt = now
        };

        transaction.StartStep(PostingStep.Record);
        return transaction;
    }

    /// <summary>Changes the transaction; <c>true</c> when the amount or date changed and Accounts has to apply it.</summary>
    public bool Change(decimal amount, DateOnly bookedOn, TransactionDescription description, TransactionNote? note)
    {
        EnsureChangeable();
        var newAmount = new Money(amount, Currency).Amount;

        Description = description;
        Note = note;
        Version++;
        if (newAmount == Amount && bookedOn == BookedOn)
        {
            return false;
        }

        Amount = newAmount;
        BookedOn = bookedOn;
        StartStep(PostingStep.Change);
        return true;
    }

    /// <summary>
    /// Deletes the transaction; <c>true</c> when Accounts has to take it off the balance first. A rejected transaction
    /// never touched the balance and is deleted at once.
    /// </summary>
    public bool Delete()
    {
        if (Status == TransactionStatus.Rejected)
        {
            Status = TransactionStatus.Deleted;
            Version++;
            return false;
        }

        EnsureChangeable();
        StartStep(PostingStep.Delete);
        return true;
    }

    /// <summary>Accounts applied the step; an answer to another step is ignored.</summary>
    public void ConfirmPosting(Guid stepId)
    {
        if (!IsPending(stepId))
        {
            return;
        }

        if (PendingStep == PostingStep.Delete)
        {
            Status = TransactionStatus.Deleted;
        }
        else
        {
            Status = TransactionStatus.Posted;
            PostedAmount = Amount;
            PostedBookedOn = BookedOn;
        }

        DeclineReason = null;
        FinishStep();
    }

    /// <summary>Accounts declined the step, so it is undone; an answer to another step is ignored.</summary>
    public void Decline(Guid stepId, DeclineReason reason)
    {
        if (!IsPending(stepId))
        {
            return;
        }

        switch (PendingStep)
        {
            case PostingStep.Record:
                Status = TransactionStatus.Rejected;
                break;
            case PostingStep.Change:
                Amount = PostedAmount;
                BookedOn = PostedBookedOn;
                Status = TransactionStatus.Posted;
                break;
            case PostingStep.Delete:
                Status = TransactionStatus.Posted;
                break;
        }

        DeclineReason = reason;
        FinishStep();
    }

    private bool IsPending(Guid stepId) => Status == TransactionStatus.Pending && PendingStepId == stepId;

    private void EnsureChangeable()
    {
        if (Status == TransactionStatus.Pending)
        {
            throw new TransactionPendingException(Id);
        }

        if (Status == TransactionStatus.Rejected)
        {
            throw new TransactionRejectedException(Id);
        }
    }

    private void StartStep(PostingStep step)
    {
        Status = TransactionStatus.Pending;
        PendingStep = step;
        PendingStepId = Guid.NewGuid();
        Version++;
        RaiseDomainEvent(new PostingRequested(
            Id,
            PendingStepId.Value,
            step,
            AccountId,
            OwnerId,
            step == PostingStep.Delete ? 0m : Amount,
            Currency,
            BookedOn));
    }

    private void FinishStep()
    {
        PendingStep = null;
        PendingStepId = null;
        Version++;
    }
}

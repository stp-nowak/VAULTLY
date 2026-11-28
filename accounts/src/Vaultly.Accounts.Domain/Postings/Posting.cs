using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Core;

namespace Vaultly.Accounts.Domain.Postings;

/// <summary>
/// What one of the user's transactions adds to an account's balance, as the Transactions service last asked for it.
/// Remembers the last applied step, so a step that arrives again changes nothing.
/// </summary>
public sealed class Posting : AggregateRoot
{
    private Posting()
    {
    }

    public TransactionId Id { get; private set; }

    public AccountId AccountId { get; private set; }

    public decimal Amount { get; private set; }

    public DateOnly BookedOn { get; private set; }

    public bool IsDeleted { get; private set; }

    /// <summary>The step (posting id from Transactions) that set the current state.</summary>
    public Guid LastStepId { get; private set; }

    /// <summary>What the posting adds to the balance: nothing once deleted.</summary>
    public decimal Effect => IsDeleted ? 0 : Amount;

    public static Posting Start(TransactionId transactionId, AccountId accountId)
        => new() { Id = transactionId, AccountId = accountId, IsDeleted = true };

    public bool HasApplied(Guid stepId) => LastStepId == stepId;

    public void Book(decimal amount, DateOnly bookedOn, Guid stepId)
    {
        Amount = amount;
        BookedOn = bookedOn;
        IsDeleted = false;
        LastStepId = stepId;
    }

    public void Delete(Guid stepId)
    {
        IsDeleted = true;
        LastStepId = stepId;
    }
}

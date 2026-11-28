using Vaultly.Core;
using Vaultly.Core.Amounts;
using Vaultly.Transactions.Domain.Accounts;

namespace Vaultly.Transactions.Domain.Transactions;

/// <summary>A step of the transaction waits to be applied to the account's balance.</summary>
/// <param name="Amount">What the transaction should add to the balance after the step; 0 when deleting.</param>
public sealed record PostingRequested(
    TransactionId TransactionId,
    Guid StepId,
    PostingStep Step,
    AccountId AccountId,
    OwnerId OwnerId,
    decimal Amount,
    Currency Currency,
    DateOnly BookedOn) : DomainEvent;

using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Postings;

/// <summary>
/// Message to this service's own queue that starts the posting saga for one step of a transaction. Sent through the
/// outbox, so the saga starts only if the step was saved.
/// </summary>
/// <param name="Amount">What the transaction adds to the balance after the step; 0 when deleting.</param>
public sealed record StartPosting(
    Guid StepId,
    Guid TransactionId,
    Guid AccountId,
    Guid OwnerId,
    PostingStep Step,
    decimal Amount,
    string Currency,
    DateOnly BookedOn);

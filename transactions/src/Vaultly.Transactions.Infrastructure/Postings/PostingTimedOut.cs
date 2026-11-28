namespace Vaultly.Transactions.Infrastructure.Postings;

/// <summary>Deferred to the saga itself: Accounts has not answered a step in time.</summary>
public sealed record PostingTimedOut(Guid StepId);

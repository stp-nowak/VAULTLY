namespace Vaultly.Accounts.PublicTypes.Commands;

/// <summary>
/// Asks Accounts to apply one step of a transaction to an account's balance: record it, change its amount or date, or
/// delete it. Accounts answers with <see cref="TransactionPosted"/> or <see cref="TransactionDeclined"/> carrying the
/// same <paramref name="StepId"/>; sending the same step again only repeats the answer.
/// </summary>
/// <param name="StepId">Identifies this step; a new step of the same transaction has a new id.</param>
/// <param name="Amount">Signed, in <paramref name="Currency"/>; ignored when deleting.</param>
public sealed record PostTransaction(
    Guid StepId,
    Guid TransactionId,
    Guid AccountId,
    Guid OwnerId,
    PostingOperation Operation,
    decimal Amount,
    string Currency,
    DateOnly BookedOn);

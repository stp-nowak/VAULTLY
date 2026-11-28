namespace Vaultly.Transactions.Domain.Transactions;

/// <summary>A change of a transaction that Accounts has to apply to the account's balance.</summary>
public enum PostingStep
{
    Record,
    Change,
    Delete
}

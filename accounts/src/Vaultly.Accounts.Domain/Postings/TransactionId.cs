namespace Vaultly.Accounts.Domain.Postings;

/// <summary>A transaction of the Transactions service; one posting belongs to each.</summary>
public readonly record struct TransactionId(Guid Value)
{
    public override string ToString() => Value.ToString();
}

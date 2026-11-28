namespace Vaultly.Transactions.Domain.Transactions;

/// <summary>The user a transaction belongs to: the <c>sub</c> of the Identity service's tokens.</summary>
public readonly record struct OwnerId(Guid Value)
{
    public override string ToString() => Value.ToString();
}

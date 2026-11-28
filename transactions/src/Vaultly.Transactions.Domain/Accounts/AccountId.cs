namespace Vaultly.Transactions.Domain.Accounts;

public readonly record struct AccountId(Guid Value)
{
    public override string ToString() => Value.ToString();
}

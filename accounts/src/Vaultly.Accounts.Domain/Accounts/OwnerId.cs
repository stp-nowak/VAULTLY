namespace Vaultly.Accounts.Domain.Accounts;

/// <summary>The user an account belongs to: the <c>sub</c> of the Identity service's tokens.</summary>
public readonly record struct OwnerId(Guid Value)
{
    public override string ToString() => Value.ToString();
}

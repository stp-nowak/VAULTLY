namespace Vaultly.Accounts.Domain.ValueObjects;

public readonly record struct AccountId(Guid Value)
{
    public static AccountId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}

/// <summary>The user an account belongs to: the <c>sub</c> of the Identity service's tokens.</summary>
public readonly record struct OwnerId(Guid Value)
{
    public override string ToString() => Value.ToString();
}

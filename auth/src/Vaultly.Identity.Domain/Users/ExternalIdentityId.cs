namespace Vaultly.Identity.Domain.Users;

public readonly record struct ExternalIdentityId(Guid Value)
{
    /// <summary>
    /// Creates a new unique external identity identifier.
    /// </summary>
    public static ExternalIdentityId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an external identity identifier from an existing GUID.
    /// </summary>
    public static ExternalIdentityId From(Guid value) => new(value);

    /// <summary>
    /// Returns the identifier as a string.
    /// </summary>
    public override string ToString() => Value.ToString();
}

namespace Vaultly.Identity.Domain.OAuthStates;

public readonly record struct OAuthStateId(Guid Value)
{
    /// <summary>
    /// Creates a new unique OAuth state identifier.
    /// </summary>
    public static OAuthStateId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an OAuth state identifier from an existing GUID.
    /// </summary>
    public static OAuthStateId From(Guid value) => new(value);

    /// <summary>
    /// Returns the identifier as a string.
    /// </summary>
    public override string ToString() => Value.ToString();
}

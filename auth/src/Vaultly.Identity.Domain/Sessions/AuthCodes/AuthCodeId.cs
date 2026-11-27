namespace Vaultly.Identity.Domain.Sessions.AuthCodes;

public readonly record struct AuthCodeId(Guid Value)
{
    /// <summary>
    /// Creates a new unique auth code identifier.
    /// </summary>
    public static AuthCodeId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an auth code identifier from an existing GUID.
    /// </summary>
    public static AuthCodeId From(Guid value) => new(value);

    /// <summary>
    /// Returns the identifier as a string.
    /// </summary>
    public override string ToString() => Value.ToString();
}

namespace Vaultly.Identity.Domain.Sessions.RefreshTokens;

public readonly record struct RefreshTokenId(Guid Value)
{
    /// <summary>
    /// Creates a new unique refresh token identifier.
    /// </summary>
    public static RefreshTokenId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a refresh token identifier from an existing GUID.
    /// </summary>
    public static RefreshTokenId From(Guid value) => new(value);

    /// <summary>
    /// Returns the identifier as a string.
    /// </summary>
    public override string ToString() => Value.ToString();
}

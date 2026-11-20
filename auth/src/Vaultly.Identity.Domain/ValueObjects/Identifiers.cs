namespace Vaultly.Identity.Domain.ValueObjects;

public readonly record struct UserId(Guid Value)
{
    /// <summary>
    /// Creates a new unique user identifier.
    /// </summary>
    public static UserId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a user identifier from an existing GUID.
    /// </summary>
    public static UserId From(Guid value) => new(value);

    /// <summary>
    /// Returns the identifier as a string.
    /// </summary>
    public override string ToString() => Value.ToString();
}

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

public readonly record struct SessionId(Guid Value)
{
    /// <summary>
    /// Creates a new unique session identifier.
    /// </summary>
    public static SessionId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a session identifier from an existing GUID.
    /// </summary>
    public static SessionId From(Guid value) => new(value);

    /// <summary>
    /// Returns the identifier as a string.
    /// </summary>
    public override string ToString() => Value.ToString();
}

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
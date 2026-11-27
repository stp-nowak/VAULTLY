namespace Vaultly.Identity.Domain.AuthorizationRequests;

public readonly record struct AuthorizationRequestId(Guid Value)
{
    /// <summary>
    /// Creates a new unique authorization request identifier.
    /// </summary>
    public static AuthorizationRequestId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an authorization request identifier from an existing GUID.
    /// </summary>
    public static AuthorizationRequestId From(Guid value) => new(value);

    /// <summary>
    /// Returns the identifier as a string.
    /// </summary>
    public override string ToString() => Value.ToString();
}

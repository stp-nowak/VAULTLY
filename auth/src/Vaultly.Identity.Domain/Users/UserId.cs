namespace Vaultly.Identity.Domain.Users;

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

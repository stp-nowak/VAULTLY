namespace Vaultly.Identity.Domain.Sessions;

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

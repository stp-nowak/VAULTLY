namespace Vaultly.Identity.Domain.AuthorizationRequests.Clients;

/// <summary>
/// Opaque `state` value chosen by the client and echoed back unchanged on the redirect (CSRF protection on the
/// client side). The authorization server never interprets it.
/// </summary>
public sealed record ClientState
{
    public const int MaxLength = 512;

    /// <summary>
    /// Creates a client state value.
    /// </summary>
    public ClientState(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidValueException("State is required.");
        }

        if (value.Length > MaxLength)
        {
            throw new InvalidValueException($"State must be {MaxLength} characters or fewer.");
        }

        Value = value;
    }

    /// <summary>Gets the state value.</summary>
    public string Value { get; }

    /// <summary>Returns the state value.</summary>
    public override string ToString() => Value;
}

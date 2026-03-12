using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record AuthMethod
{
    /// <summary>
    /// Creates a new authentication method value object.
    /// </summary>
    public AuthMethod(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Auth method is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 50)
        {
            throw new InvalidValueException("Auth method must be 50 characters or fewer.");
        }

        Value = trimmed;
    }

    /// <summary>
    /// Gets the auth method value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the auth method as a string.
    /// </summary>
    public override string ToString() => Value;
}
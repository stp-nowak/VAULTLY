using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record AuthMethod
{
    public const int MaxLength = 50;

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
        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"Auth method must be {MaxLength} characters or fewer.");
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
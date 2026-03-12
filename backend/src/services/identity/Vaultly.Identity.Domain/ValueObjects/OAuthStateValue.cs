using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record OAuthStateValue
{
    /// <summary>
    /// Creates a new OAuth state value object.
    /// </summary>
    public OAuthStateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("OAuth state is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 128)
        {
            throw new InvalidValueException("OAuth state must be 128 characters or fewer.");
        }

        Value = trimmed;
    }

    /// <summary>
    /// Gets the OAuth state value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the OAuth state as a string.
    /// </summary>
    public override string ToString() => Value;
}
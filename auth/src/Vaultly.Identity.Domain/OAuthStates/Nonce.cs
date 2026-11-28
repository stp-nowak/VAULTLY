using Vaultly.Core;

namespace Vaultly.Identity.Domain.OAuthStates;

public sealed record Nonce
{
    public const int MaxLength = 128;

    /// <summary>
    /// Creates a new nonce value object.
    /// </summary>
    public Nonce(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Nonce is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"Nonce must be {MaxLength} characters or fewer.");
        }

        Value = trimmed;
    }

    /// <summary>
    /// Gets the nonce value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the nonce as a string.
    /// </summary>
    public override string ToString() => Value;
}

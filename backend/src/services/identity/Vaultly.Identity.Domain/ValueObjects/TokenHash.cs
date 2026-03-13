using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record TokenHash
{
    public const int MaxLength = 128;

    /// <summary>
    /// Creates a new token hash value object.
    /// </summary>
    public TokenHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Token hash is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"Token hash must be {MaxLength} characters or fewer.");
        }

        Value = trimmed;
    }

    /// <summary>
    /// Gets the token hash value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the token hash as a string.
    /// </summary>
    public override string ToString() => Value;
}
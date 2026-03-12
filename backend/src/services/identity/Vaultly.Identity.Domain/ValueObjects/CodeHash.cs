using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record CodeHash
{
    /// <summary>
    /// Creates a new auth code hash value object.
    /// </summary>
    public CodeHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Auth code hash is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 128)
        {
            throw new InvalidValueException("Auth code hash must be 128 characters or fewer.");
        }

        Value = trimmed;
    }

    /// <summary>
    /// Gets the auth code hash value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the auth code hash as a string.
    /// </summary>
    public override string ToString() => Value;
}
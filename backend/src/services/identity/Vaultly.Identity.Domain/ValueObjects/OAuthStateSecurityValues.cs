using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record CodeVerifier
{
    /// <summary>
    /// Creates a new code verifier value object.
    /// </summary>
    public CodeVerifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Code verifier is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 128)
        {
            throw new InvalidValueException("Code verifier must be 128 characters or fewer.");
        }

        Value = trimmed;
    }

    /// <summary>
    /// Gets the code verifier value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the code verifier as a string.
    /// </summary>
    public override string ToString() => Value;
}

public sealed record Nonce
{
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
        if (trimmed.Length > 128)
        {
            throw new InvalidValueException("Nonce must be 128 characters or fewer.");
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
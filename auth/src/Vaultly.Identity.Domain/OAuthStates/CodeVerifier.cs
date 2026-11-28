using Vaultly.Core;

namespace Vaultly.Identity.Domain.OAuthStates;

public sealed record CodeVerifier
{
    public const int MaxLength = 128;

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
        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"Code verifier must be {MaxLength} characters or fewer.");
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

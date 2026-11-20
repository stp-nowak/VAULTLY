using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record ProviderUserId
{
    public const int MaxLength = 200;

    /// <summary>
    /// Creates a new provider user identifier value object.
    /// </summary>
    public ProviderUserId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Provider user id is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"Provider user id must be {MaxLength} characters or fewer.");
        }

        Value = trimmed;
    }

    /// <summary>
    /// Gets the provider user id value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the provider user id as a string.
    /// </summary>
    public override string ToString() => Value;
}
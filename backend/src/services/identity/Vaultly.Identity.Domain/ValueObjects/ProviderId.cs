using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record ProviderId
{
    public const int MaxLength = 50;

    /// <summary>
    /// Creates a new provider identifier value object.
    /// </summary>
    public ProviderId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Provider id is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"Provider id must be {MaxLength} characters or fewer.");
        }

        Value = trimmed.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the provider id value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the provider id as a string.
    /// </summary>
    public override string ToString() => Value;
}
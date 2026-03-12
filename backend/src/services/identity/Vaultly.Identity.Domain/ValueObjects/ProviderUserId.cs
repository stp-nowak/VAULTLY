using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record ProviderUserId
{
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
        if (trimmed.Length > 200)
        {
            throw new InvalidValueException("Provider user id must be 200 characters or fewer.");
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
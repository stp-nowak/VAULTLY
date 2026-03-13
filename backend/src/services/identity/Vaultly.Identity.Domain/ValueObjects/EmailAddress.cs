using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record EmailAddress
{
    public const int MaxLength = 320;

    /// <summary>
    /// Creates a new email address value object.
    /// </summary>
    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Email is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"Email must be {MaxLength} characters or fewer.");
        }

        if (!trimmed.Contains('@'))
        {
            throw new InvalidValueException($"Email '{value}' is invalid.");
        }

        Value = trimmed.ToLowerInvariant();
    }

    /// <summary>
    /// Gets the normalized email value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the email address as a string.
    /// </summary>
    public override string ToString() => Value;
}
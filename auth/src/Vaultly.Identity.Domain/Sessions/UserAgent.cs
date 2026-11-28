using Vaultly.Core;

namespace Vaultly.Identity.Domain.Sessions;

public sealed record UserAgent
{
    public const int MaxLength = 512;

    /// <summary>
    /// Creates a new user agent value object.
    /// </summary>
    public UserAgent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("User agent is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"User agent must be {MaxLength} characters or fewer.");
        }

        Value = trimmed;
    }

    /// <summary>
    /// Gets the user agent value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a user agent value object or returns null when no value is provided.
    /// </summary>
    public static UserAgent? FromNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : new UserAgent(value);

    /// <summary>
    /// Returns the user agent as a string.
    /// </summary>
    public override string ToString() => Value;
}

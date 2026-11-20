using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

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

public sealed record IpAddress
{
    public const int MaxLength = 64;

    /// <summary>
    /// Creates a new IP address value object.
    /// </summary>
    public IpAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("IP address is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"IP address must be {MaxLength} characters or fewer.");
        }

        Value = trimmed;
    }

    /// <summary>
    /// Gets the IP address value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates an IP address value object or returns null when no value is provided.
    /// </summary>
    public static IpAddress? FromNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : new IpAddress(value);

    /// <summary>
    /// Returns the IP address as a string.
    /// </summary>
    public override string ToString() => Value;
}
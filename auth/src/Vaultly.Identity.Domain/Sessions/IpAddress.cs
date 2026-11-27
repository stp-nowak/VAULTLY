namespace Vaultly.Identity.Domain.Sessions;

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

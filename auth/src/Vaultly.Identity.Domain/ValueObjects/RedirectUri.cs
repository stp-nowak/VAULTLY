using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record RedirectUri
{
    public const int MaxLength = 2048;

    /// <summary>
    /// Creates a new redirect URI value object.
    /// </summary>
    public RedirectUri(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Redirect URI is required.");
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidValueException("Redirect URI must be an absolute URI.");
        }

        var normalizedValue = uri.AbsoluteUri;
        if (normalizedValue.Length > MaxLength)
        {
            throw new InvalidValueException($"Redirect URI must be {MaxLength} characters or fewer.");
        }

        Value = normalizedValue;
    }

    /// <summary>
    /// Gets the normalized redirect URI value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a redirect URI value object or returns null when no value is provided.
    /// </summary>
    public static RedirectUri? FromNullable(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : new RedirectUri(value);

    /// <summary>
    /// Returns the redirect URI as a string.
    /// </summary>
    public override string ToString() => Value;
}

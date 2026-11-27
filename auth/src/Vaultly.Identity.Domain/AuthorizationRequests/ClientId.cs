namespace Vaultly.Identity.Domain.AuthorizationRequests;

/// <summary>
/// Identifier of a registered OAuth/OIDC client application (e.g. the web app's backend).
/// </summary>
public sealed record ClientId
{
    public const int MaxLength = 100;

    /// <summary>
    /// Creates a client identifier; only URL-safe characters are allowed because it travels in query strings.
    /// </summary>
    public ClientId(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new InvalidValueException("Client id is required.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"Client id must be {MaxLength} characters or fewer.");
        }

        if (!trimmed.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))
        {
            throw new InvalidValueException("Client id contains invalid characters.");
        }

        Value = trimmed;
    }

    /// <summary>Gets the client identifier.</summary>
    public string Value { get; }

    /// <summary>Returns the client identifier.</summary>
    public override string ToString() => Value;
}

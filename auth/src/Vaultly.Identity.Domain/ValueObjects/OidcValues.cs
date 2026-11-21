using System.Security.Cryptography;
using System.Text;

using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

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

/// <summary>
/// Opaque `state` value chosen by the client and echoed back unchanged on the redirect (CSRF protection on the
/// client side). The authorization server never interprets it.
/// </summary>
public sealed record ClientState
{
    public const int MaxLength = 512;

    /// <summary>
    /// Creates a client state value.
    /// </summary>
    public ClientState(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidValueException("State is required.");
        }

        if (value.Length > MaxLength)
        {
            throw new InvalidValueException($"State must be {MaxLength} characters or fewer.");
        }

        Value = value;
    }

    /// <summary>Gets the state value.</summary>
    public string Value { get; }

    /// <summary>Returns the state value.</summary>
    public override string ToString() => Value;
}

/// <summary>
/// RFC 7636 PKCE code challenge using the <c>S256</c> method (the only method accepted: <c>plain</c> gives no
/// protection against an intercepted authorization code).
/// </summary>
public sealed record CodeChallenge
{
    /// <summary>A SHA-256 hash in unpadded base64url is always 43 characters.</summary>
    public const int Length = 43;

    /// <summary>The only supported challenge method.</summary>
    public const string MethodS256 = "S256";

    /// <summary>
    /// Creates an S256 code challenge.
    /// </summary>
    public CodeChallenge(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != Length || !value.All(IsBase64UrlChar))
        {
            throw new InvalidValueException("Code challenge must be a base64url-encoded SHA-256 hash.");
        }

        Value = value;
    }

    /// <summary>Gets the challenge value.</summary>
    public string Value { get; }

    /// <summary>
    /// Checks a code verifier against this challenge: <c>BASE64URL(SHA256(verifier)) == challenge</c>, compared in
    /// constant time. Verifier format (43–128 unreserved characters) is enforced as required by RFC 7636.
    /// </summary>
    public bool IsSatisfiedBy(string? codeVerifier)
    {
        if (string.IsNullOrEmpty(codeVerifier) || codeVerifier.Length is < 43 or > 128 || !codeVerifier.All(IsUnreservedChar))
        {
            return false;
        }

        var computed = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(computed), Encoding.ASCII.GetBytes(Value));
    }

    /// <summary>Returns the challenge value.</summary>
    public override string ToString() => Value;

    private static bool IsBase64UrlChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '_';

    private static bool IsUnreservedChar(char c) => char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '~';
}

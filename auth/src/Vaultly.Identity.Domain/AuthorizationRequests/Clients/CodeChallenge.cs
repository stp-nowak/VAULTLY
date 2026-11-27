using System.Security.Cryptography;
using System.Text;

namespace Vaultly.Identity.Domain.AuthorizationRequests.Clients;

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

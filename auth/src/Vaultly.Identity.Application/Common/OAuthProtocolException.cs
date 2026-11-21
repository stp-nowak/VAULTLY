namespace Vaultly.Identity.Application.Common;

/// <summary>
/// An OAuth 2.0 protocol error (RFC 6749 §5.2 / §4.1.2.1). The API layer turns it into the standard
/// <c>{ error, error_description }</c> response or error redirect. It is not a domain rule violation, so it does not
/// derive from <c>DomainException</c>.
/// </summary>
public sealed class OAuthProtocolException(string error, string description, int statusCode = 400) : Exception(description)
{
    /// <summary>RFC 6749 error code, e.g. <c>invalid_grant</c>.</summary>
    public string Error { get; } = error;

    /// <summary>HTTP status for direct (non-redirect) responses.</summary>
    public int StatusCode { get; } = statusCode;

    public static OAuthProtocolException InvalidRequest(string description) => new("invalid_request", description);

    /// <summary>Unknown client or wrong secret: 401 as required for client authentication failures.</summary>
    public static OAuthProtocolException InvalidClient() => new("invalid_client", "Client authentication failed.", 401);

    public static OAuthProtocolException InvalidGrant(string description) => new("invalid_grant", description);

    public static OAuthProtocolException UnsupportedGrantType() => new("unsupported_grant_type", "Grant type is not supported.");
}

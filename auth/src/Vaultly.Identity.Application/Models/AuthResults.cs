namespace Vaultly.Identity.Application.Models;

/// <summary>
/// Where to send the browser after <c>/authorize</c>: back to the client (with a code or an error) or to the login UI.
/// </summary>
public sealed record AuthorizationResult(string RedirectUrl);

/// <summary>
/// Result of a completed external login: redirect back to the client plus the new SSO session for the cookie.
/// </summary>
public sealed record AuthCallbackResult(string RedirectUrl, string SsoToken, DateTimeOffset SsoExpiresAt);

/// <summary>
/// Tokens issued by the token endpoint (authorization_code and refresh_token grants).
/// </summary>
public sealed record TokenResult(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken,
    int RefreshExpiresInSeconds,
    Guid SessionId,
    AuthenticatedUserDto User);

/// <summary>
/// Where to send the browser after the SSO session ended.
/// </summary>
public sealed record EndSessionResult(string RedirectUrl);

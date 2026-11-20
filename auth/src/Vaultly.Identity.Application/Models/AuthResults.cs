namespace Vaultly.Identity.Application.Models;

/// <summary>
/// Represents the result of a successful OAuth callback handling.
/// </summary>
public sealed record AuthCallbackResult(string RedirectUrl);

/// <summary>
/// Represents the result of exchanging an auth code for tokens.
/// </summary>
public sealed record ExchangeResult(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid SessionId,
    AuthenticatedUserDto User,
    string? CookieDomain);

/// <summary>
/// Represents the result of a refresh token rotation attempt.
/// </summary>
public sealed record RefreshResult(
    bool Success,
    string? AccessToken,
    int ExpiresInSeconds,
    string? RefreshToken,
    DateTimeOffset? RefreshTokenExpiresAt,
    Guid? SessionId,
    AuthenticatedUserDto? User,
    string? CookieDomain)
{
    /// <summary>
    /// Creates a failed refresh result.
    /// </summary>
    public static RefreshResult Failed(string? cookieDomain) => new(false, null, 0, null, null, null, null, cookieDomain);

    /// <summary>
    /// Creates a successful refresh result.
    /// </summary>
    public static RefreshResult Succeeded(
        string accessToken,
        int expiresInSeconds,
        string refreshToken,
        DateTimeOffset refreshTokenExpiresAt,
        Guid sessionId,
        AuthenticatedUserDto user,
        string? cookieDomain)
        => new(true, accessToken, expiresInSeconds, refreshToken, refreshTokenExpiresAt, sessionId, user, cookieDomain);
}
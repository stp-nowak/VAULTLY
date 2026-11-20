using System.Security.Cryptography;

using Microsoft.AspNetCore.WebUtilities;

using Vaultly.Web.Api.Features.Auth.IdentityService;

namespace Vaultly.Web.Api.Features.Auth.Sessions;

/// <summary>
/// Server-side state of one browser session, stored in Redis under the opaque id carried by the session cookie.
/// </summary>
public sealed record WebSession(
    AuthenticatedUser User,
    Guid AuthSessionId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// Creates a session right after a successful code exchange.
    /// </summary>
    public static WebSession Create(IdentityTokenSet tokens, DateTimeOffset now)
        => new(
            tokens.User,
            tokens.AuthSessionId,
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAt,
            now);

    /// <summary>
    /// Applies rotated tokens from a refresh; the refresh token is single-use on the Identity side.
    /// </summary>
    public WebSession WithRotatedTokens(IdentityTokenSet tokens)
        => this with
        {
            User = tokens.User,
            AccessToken = tokens.AccessToken,
            AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
            RefreshToken = tokens.RefreshToken,
            RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt,
        };

    /// <summary>
    /// Whether the access token must be refreshed before being forwarded.
    /// </summary>
    public bool AccessTokenExpiresWithin(TimeSpan window, DateTimeOffset now) => AccessTokenExpiresAt - window <= now;
}

/// <summary>
/// Generates opaque session identifiers.
/// </summary>
public static class WebSessionId
{
    /// <summary>
    /// Creates a 256-bit random, URL-safe identifier. It is a bearer credential: never log it.
    /// </summary>
    public static string New() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
}

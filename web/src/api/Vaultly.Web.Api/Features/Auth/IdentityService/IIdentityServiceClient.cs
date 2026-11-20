namespace Vaultly.Web.Api.Features.Auth.IdentityService;

/// <summary>
/// Server-to-server client of the Identity service (<c>./auth</c>). The web API is the only OAuth client the
/// browser talks to; tokens obtained here are stored server-side and never sent to the SPA.
/// </summary>
public interface IIdentityServiceClient
{
    /// <summary>
    /// Exchanges an authorization code (from the OAuth callback) for an access and refresh token.
    /// </summary>
    Task<IdentityTokenSet> ExchangeCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// Rotates tokens with a refresh token. Returns <c>null</c> when the refresh token is rejected (session ended).
    /// </summary>
    Task<IdentityTokenSet?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Ends the Identity session that owns the refresh token.
    /// </summary>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Lists the user's Identity sessions.
    /// </summary>
    Task<IReadOnlyList<IdentitySession>> GetSessionsAsync(string accessToken, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes one of the user's Identity sessions. Returns <c>false</c> when it does not exist.
    /// </summary>
    Task<bool> RevokeSessionAsync(string accessToken, Guid sessionId, CancellationToken cancellationToken);
}

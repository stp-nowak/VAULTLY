using Vaultly.Identity.ApiContracts;

namespace Vaultly.Web.Api.Features.Auth.IdentityService;

/// <summary>
/// Server-to-server client of the Identity service (<c>./auth</c>). Tokens obtained here stay on the server.
/// </summary>
public interface IIdentityServiceClient
{
    /// <summary>Authorization code grant with the PKCE verifier and the redirect URI used at <c>/authorize</c>.</summary>
    Task<IdentityTokenSet> ExchangeCodeAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken);

    /// <summary>Rotates tokens; <c>null</c> when the refresh token is rejected (the session has ended).</summary>
    Task<IdentityTokenSet?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>Revokes the refresh token, which ends the Identity session.</summary>
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);

    Task<IReadOnlyList<SessionResponse>> GetSessionsAsync(string accessToken, CancellationToken cancellationToken);

    /// <summary>Returns <c>false</c> when the session does not exist.</summary>
    Task<bool> RevokeSessionAsync(string accessToken, Guid sessionId, CancellationToken cancellationToken);
}

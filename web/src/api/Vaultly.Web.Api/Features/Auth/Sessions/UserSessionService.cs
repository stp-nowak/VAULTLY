using Microsoft.Extensions.Options;

using Vaultly.Web.Api.Features.Auth.IdentityService;

namespace Vaultly.Web.Api.Features.Auth.Sessions;

/// <summary>
/// Public contract of the Auth feature for other features (e.g. Web API proxy): a valid access token for the
/// caller's session, refreshed transparently.
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Returns a non-expired access token for the current browser session, or <c>null</c> when the user is not
    /// signed in or the session has ended (the session cookie is cleared in that case).
    /// </summary>
    Task<string?> GetAccessTokenAsync(HttpContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Lifecycle of browser sessions: sign-in, lookup with token refresh, sign-out.
/// </summary>
public interface IUserSessionService
{
    /// <summary>Creates a session for freshly issued tokens and sets the session cookie.</summary>
    Task SignInAsync(HttpContext context, IdentityTokenSet tokens, CancellationToken cancellationToken);

    /// <summary>Returns the active session with a usable access token, or <c>null</c>.</summary>
    Task<WebSession?> GetActiveSessionAsync(HttpContext context, CancellationToken cancellationToken);

    /// <summary>Ends the session locally and at the Identity service.</summary>
    Task SignOutAsync(HttpContext context, CancellationToken cancellationToken);

    /// <summary>Ends the session locally only (the Identity session is already revoked).</summary>
    Task ForgetAsync(HttpContext context, CancellationToken cancellationToken);
}

/// <inheritdoc cref="IUserSessionService" />
public sealed class UserSessionService(
    IWebSessionStore store,
    SessionCookie cookie,
    IIdentityServiceClient identity,
    SessionRefreshLock refreshLock,
    TimeProvider timeProvider,
    IOptions<AuthOptions> options,
    ILogger<UserSessionService> logger) : IUserSessionService, IAccessTokenProvider
{
    private readonly IWebSessionStore _store = store;
    private readonly SessionCookie _cookie = cookie;
    private readonly IIdentityServiceClient _identity = identity;
    private readonly SessionRefreshLock _refreshLock = refreshLock;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly TimeSpan _refreshSkew = TimeSpan.FromSeconds(options.Value.AccessTokenRefreshSkewSeconds);
    private readonly ILogger<UserSessionService> _logger = logger;

    /// <inheritdoc />
    public async Task SignInAsync(HttpContext context, IdentityTokenSet tokens, CancellationToken cancellationToken)
    {
        // Never reuse a pre-existing id (session fixation): drop the old session and issue a new identifier.
        if (_cookie.Read(context.Request) is { } previousSessionId)
        {
            await _store.RemoveAsync(previousSessionId, cancellationToken);
        }

        var sessionId = WebSessionId.New();
        var session = WebSession.Create(tokens, _timeProvider.GetUtcNow());
        await _store.SetAsync(sessionId, session, cancellationToken);
        _cookie.Append(context.Response, sessionId, session.RefreshTokenExpiresAt);

        _logger.LogInformation(
            "Signed in user {UserId} with identity session {AuthSessionId}",
            session.User.Id,
            session.AuthSessionId);
    }

    /// <inheritdoc />
    public async Task<WebSession?> GetActiveSessionAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var sessionId = _cookie.Read(context.Request);
        if (sessionId is null)
        {
            return null;
        }

        var session = await _store.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            _cookie.Delete(context.Response);
            return null;
        }

        if (!session.AccessTokenExpiresWithin(_refreshSkew, _timeProvider.GetUtcNow()))
        {
            return session;
        }

        return await RefreshAsync(context, sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(HttpContext context, CancellationToken cancellationToken)
        => (await GetActiveSessionAsync(context, cancellationToken))?.AccessToken;

    /// <inheritdoc />
    public async Task SignOutAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var sessionId = _cookie.Read(context.Request);
        if (sessionId is not null && await _store.GetAsync(sessionId, cancellationToken) is { } session)
        {
            try
            {
                await _identity.LogoutAsync(session.RefreshToken, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or IdentityServiceException)
            {
                // Local sign-out must still succeed; the Identity session expires with its refresh token.
                _logger.LogWarning(exception, "Identity logout failed for session {AuthSessionId}", session.AuthSessionId);
            }

            _logger.LogInformation("Signed out user {UserId}", session.User.Id);
        }

        await ForgetAsync(context, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ForgetAsync(HttpContext context, CancellationToken cancellationToken)
    {
        if (_cookie.Read(context.Request) is { } sessionId)
        {
            await _store.RemoveAsync(sessionId, cancellationToken);
        }

        _cookie.Delete(context.Response);
    }

    /// <summary>
    /// Rotates tokens under a per-session lock. Refresh tokens are single-use, so parallel requests must not refresh
    /// concurrently; the session is re-read after acquiring the lock because another request may have refreshed it.
    /// </summary>
    private async Task<WebSession?> RefreshAsync(HttpContext context, string sessionId, CancellationToken cancellationToken)
    {
        using var _ = await _refreshLock.AcquireAsync(sessionId, cancellationToken);

        var session = await _store.GetAsync(sessionId, cancellationToken);
        var now = _timeProvider.GetUtcNow();
        if (session is null)
        {
            _cookie.Delete(context.Response);
            return null;
        }

        if (!session.AccessTokenExpiresWithin(_refreshSkew, now))
        {
            return session;
        }

        var tokens = session.RefreshTokenExpiresAt > now
            ? await _identity.RefreshAsync(session.RefreshToken, cancellationToken)
            : null;

        if (tokens is null)
        {
            _logger.LogInformation("Identity session {AuthSessionId} ended; removing web session", session.AuthSessionId);
            await _store.RemoveAsync(sessionId, cancellationToken);
            _cookie.Delete(context.Response);
            return null;
        }

        var refreshed = session.WithRotatedTokens(tokens);
        await _store.SetAsync(sessionId, refreshed, cancellationToken);
        return refreshed;
    }
}

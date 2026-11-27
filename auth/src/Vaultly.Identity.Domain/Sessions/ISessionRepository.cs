using Vaultly.Identity.Domain.Sessions.AuthCodes;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Domain.Sessions;

public interface ISessionRepository
{
    /// <summary>
    /// Adds a new session aggregate to the repository.
    /// </summary>
    void Add(Session session);

    /// <summary>
    /// Gets a session with tokens for a specific user.
    /// </summary>
    Task<Session?> GetWithTokensForUserAsync(SessionId sessionId, UserId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a session by an auth code hash.
    /// </summary>
    Task<Session?> GetByAuthCodeHashAsync(CodeHash codeHash, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a session by a refresh token hash.
    /// </summary>
    Task<Session?> GetByRefreshTokenHashAsync(TokenHash tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a session by the hash of its single sign-on token.
    /// </summary>
    Task<Session?> GetBySsoTokenHashAsync(TokenHash ssoTokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a user's sessions that are not revoked, with their refresh tokens, so they can be ended.
    /// </summary>
    Task<IReadOnlyList<Session>> GetActiveForUserAsync(UserId userId, CancellationToken cancellationToken);
}

using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Interfaces.Repositories;

public interface ISessionRepository
{
    /// <summary>
    /// Adds a new session aggregate to the repository.
    /// </summary>
    void Add(Session session);

    /// <summary>
    /// Gets all sessions for the specified user.
    /// </summary>
    Task<IReadOnlyList<Session>> GetForUserAsync(UserId userId, CancellationToken cancellationToken);

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
}
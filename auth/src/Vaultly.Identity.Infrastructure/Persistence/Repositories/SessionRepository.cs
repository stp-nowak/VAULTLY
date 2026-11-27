using Microsoft.EntityFrameworkCore;


using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for session aggregates backed by the identity DbContext.
/// </summary>
public sealed class SessionRepository(IdentityDbContext dbContext) : ISessionRepository
{
    private readonly IdentityDbContext _dbContext = dbContext;

    /// <summary>
    /// Adds a new session aggregate to the EF Core change tracker.
    /// </summary>
    public void Add(Session session) => _dbContext.Sessions.Add(session);

    /// <summary>
    /// Gets a session with tokens for a specific user.
    /// </summary>
    public Task<Session?> GetWithTokensForUserAsync(SessionId sessionId, UserId userId, CancellationToken cancellationToken)
        => _dbContext.Sessions
            .Include(x => x.RefreshTokens)
            .Include(x => x.AuthCodes)
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);

    /// <summary>
    /// Gets a session by an auth code hash.
    /// </summary>
    public Task<Session?> GetByAuthCodeHashAsync(CodeHash codeHash, CancellationToken cancellationToken)
        => _dbContext.Sessions
            .Include(x => x.RefreshTokens)
            .Include(x => x.AuthCodes)
            .SingleOrDefaultAsync(x => x.AuthCodes.Any(code => code.CodeHash == codeHash), cancellationToken);

    /// <summary>
    /// Gets a session by a refresh token hash.
    /// </summary>
    public Task<Session?> GetByRefreshTokenHashAsync(TokenHash tokenHash, CancellationToken cancellationToken)
        => _dbContext.Sessions
            .Include(x => x.RefreshTokens)
            .Include(x => x.AuthCodes)
            .SingleOrDefaultAsync(x => x.RefreshTokens.Any(token => token.TokenHash == tokenHash), cancellationToken);

    /// <summary>
    /// Gets a session by the hash of its SSO token (auth codes are loaded so a new one can be issued).
    /// </summary>
    public Task<Session?> GetBySsoTokenHashAsync(TokenHash ssoTokenHash, CancellationToken cancellationToken)
        => _dbContext.Sessions
            .Include(x => x.RefreshTokens)
            .Include(x => x.AuthCodes)
            .SingleOrDefaultAsync(x => x.SsoTokenHash == ssoTokenHash, cancellationToken);

    public async Task<IReadOnlyList<Session>> GetActiveForUserAsync(UserId userId, CancellationToken cancellationToken)
        => await _dbContext.Sessions
            .Include(x => x.RefreshTokens)
            .Where(x => x.UserId == userId && !x.IsRevoked)
            .ToListAsync(cancellationToken);
}

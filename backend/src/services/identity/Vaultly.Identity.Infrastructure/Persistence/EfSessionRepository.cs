using Microsoft.EntityFrameworkCore;

using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Infrastructure.Persistence;

/// <summary>
/// EF Core repository for session aggregates.
/// </summary>
public sealed class EfSessionRepository(IdentityDbContext dbContext) : ISessionRepository
{
    private readonly IdentityDbContext _dbContext = dbContext;

    /// <summary>
    /// Adds a new session aggregate to the EF Core change tracker.
    /// </summary>
    public void Add(Session session) => _dbContext.Sessions.Add(session);

    /// <summary>
    /// Gets all sessions for the specified user.
    /// </summary>
    public async Task<IReadOnlyList<Session>> GetForUserAsync(UserId userId, CancellationToken cancellationToken)
        => await _dbContext.Sessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastUsedAt)
            .ToListAsync(cancellationToken);

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
}
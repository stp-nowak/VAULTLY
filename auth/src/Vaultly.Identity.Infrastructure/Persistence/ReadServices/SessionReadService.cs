using Microsoft.EntityFrameworkCore;

using Vaultly.Identity.Application.Interfaces.ReadServices;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Infrastructure.Persistence.ReadServices;

/// <summary>
/// EF Core implementation of the session read side. Uses <c>AsNoTracking</c> because results are
/// projected to DTOs and must never be written back through the change tracker.
/// </summary>
public sealed class SessionReadService(IdentityDbContext dbContext) : ISessionReadService
{
    private readonly IdentityDbContext _dbContext = dbContext;

    /// <summary>
    /// Gets all sessions of a user ordered by last use, flagging the caller's current session.
    /// </summary>
    public async Task<IReadOnlyList<SessionDto>> GetForUserAsync(
        Guid userId,
        Guid currentSessionId,
        CancellationToken cancellationToken)
    {
        var ownerId = UserId.From(userId);

        // Value objects are unwrapped in the final projection; EF evaluates it after materialising the converted columns.
        return await _dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.UserId == ownerId)
            .OrderByDescending(x => x.LastUsedAt)
            .Select(x => new SessionDto(
                x.Id.Value,
                x.CreatedAt,
                x.LastUsedAt,
                x.UserAgent == null ? null : x.UserAgent.Value,
                x.IpAddress == null ? null : x.IpAddress.Value,
                x.IsRevoked,
                x.Id.Value == currentSessionId))
            .ToListAsync(cancellationToken);
    }
}

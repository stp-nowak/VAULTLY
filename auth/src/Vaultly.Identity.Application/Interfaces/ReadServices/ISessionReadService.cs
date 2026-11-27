
using Vaultly.Identity.Application.Features.Sessions.GetSessions;

namespace Vaultly.Identity.Application.Interfaces.ReadServices;

/// <summary>
/// Read side (CQRS) for sessions. Returns DTOs straight from storage without materialising
/// <c>Session</c> aggregates, so queries stay cheap and cannot accidentally mutate domain state.
/// </summary>
public interface ISessionReadService
{
    /// <summary>
    /// Gets all sessions of a user ordered by last use (most recent first).
    /// </summary>
    /// <param name="userId">Owner of the sessions.</param>
    /// <param name="currentSessionId">Session of the caller, flagged with <see cref="SessionDto.IsCurrent"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<SessionDto>> GetForUserAsync(Guid userId, Guid currentSessionId, CancellationToken cancellationToken);
}

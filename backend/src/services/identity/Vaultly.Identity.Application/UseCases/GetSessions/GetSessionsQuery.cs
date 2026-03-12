using MediatR;

using Vaultly.Identity.Application.Interfaces.Repositories;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.UseCases.GetSessions;

/// <summary>
/// Query to retrieve active sessions for a user.
/// </summary>
public sealed record GetSessionsQuery(Guid UserId, Guid CurrentSessionId) : IRequest<IReadOnlyList<SessionDto>>;

/// <summary>
/// Handles retrieval of user sessions.
/// </summary>
public sealed class GetSessionsHandler(ISessionRepository sessionRepository)
    : IRequestHandler<GetSessionsQuery, IReadOnlyList<SessionDto>>
{
    private readonly ISessionRepository _sessionRepository = sessionRepository;

    /// <summary>
    /// Gets the sessions for the requested user.
    /// </summary>
    public async Task<IReadOnlyList<SessionDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _sessionRepository.GetForUserAsync(UserId.From(request.UserId), cancellationToken);

        return sessions
            .Select(x => new SessionDto(
                x.Id.Value,
                x.CreatedAt,
                x.LastUsedAt,
                x.UserAgent?.Value,
                x.IpAddress?.Value,
                x.IsRevoked,
                x.Id.Value == request.CurrentSessionId))
            .ToList();
    }
}
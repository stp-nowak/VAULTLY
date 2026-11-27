using Vaultly.Core.Messaging;
using Vaultly.Identity.Application.Interfaces.ReadServices;

namespace Vaultly.Identity.Application.Features.Sessions.GetSessions;

/// <summary>
/// Handles retrieval of user sessions through the read service (no aggregates, no unit of work).
/// </summary>
public sealed class GetSessionsHandler(ISessionReadService sessionReadService)
    : IQueryHandler<GetSessionsQuery, IReadOnlyList<SessionDto>>
{
    private readonly ISessionReadService _sessionReadService = sessionReadService;

    /// <summary>
    /// Gets the sessions for the requested user.
    /// </summary>
    public Task<IReadOnlyList<SessionDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
        => _sessionReadService.GetForUserAsync(request.UserId, request.CurrentSessionId, cancellationToken);
}

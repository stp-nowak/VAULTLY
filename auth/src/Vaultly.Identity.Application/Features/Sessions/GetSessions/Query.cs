using Vaultly.Identity.Application.Messaging;

namespace Vaultly.Identity.Application.Features.Sessions.GetSessions;

/// <summary>
/// Query to retrieve active sessions for a user.
/// </summary>
public sealed record GetSessionsQuery(Guid UserId, Guid CurrentSessionId) : IQuery<IReadOnlyList<SessionDto>>;

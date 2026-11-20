using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.Application.Models;

namespace Vaultly.Identity.Application.Queries.GetSessions;

/// <summary>
/// Query to retrieve active sessions for a user.
/// </summary>
public sealed record GetSessionsQuery(Guid UserId, Guid CurrentSessionId) : IQuery<IReadOnlyList<SessionDto>>;

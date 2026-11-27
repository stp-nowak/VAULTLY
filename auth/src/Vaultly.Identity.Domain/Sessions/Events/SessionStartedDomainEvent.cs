using Vaultly.Core;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Domain.Sessions.Events;

/// <summary>
/// Raised when a new session starts for a user.
/// </summary>
public sealed record SessionStartedDomainEvent(
    SessionId SessionId,
    UserId UserId,
    AuthMethod AuthMethod) : DomainEvent;

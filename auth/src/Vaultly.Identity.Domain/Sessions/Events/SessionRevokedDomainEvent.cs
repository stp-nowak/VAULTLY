using Vaultly.Core;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Domain.Sessions.Events;

/// <summary>
/// Raised when a session is revoked.
/// </summary>
public sealed record SessionRevokedDomainEvent(
    SessionId SessionId,
    UserId UserId) : DomainEvent;

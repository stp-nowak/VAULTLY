using Vaultly.Core;

namespace Vaultly.Identity.Domain.Sessions.AuthCodes.Events;

/// <summary>
/// Raised when an auth code is issued for a session.
/// </summary>
public sealed record AuthCodeIssued(
    SessionId SessionId,
    AuthCodeId AuthCodeId,
    DateTimeOffset ExpiresAt) : DomainEvent;

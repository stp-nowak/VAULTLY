using Vaultly.Core;

namespace Vaultly.Identity.Domain.Sessions.AuthCodes.Events;

/// <summary>
/// Raised when an auth code is consumed successfully.
/// </summary>
public sealed record AuthCodeConsumed(
    SessionId SessionId,
    AuthCodeId AuthCodeId,
    DateTimeOffset UsedAt) : DomainEvent;

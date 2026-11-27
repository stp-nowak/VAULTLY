using Vaultly.Core;

namespace Vaultly.Identity.Domain.Sessions.AuthCodes;

/// <summary>
/// Raised when an auth code is issued for a session.
/// </summary>
public sealed record SessionAuthCodeIssuedDomainEvent(
    SessionId SessionId,
    AuthCodeId AuthCodeId,
    DateTimeOffset ExpiresAt) : DomainEvent;

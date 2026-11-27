using Vaultly.Core;

namespace Vaultly.Identity.Domain.Sessions.AuthCodes;

/// <summary>
/// Raised when an auth code is consumed successfully.
/// </summary>
public sealed record SessionAuthCodeConsumedDomainEvent(
    SessionId SessionId,
    AuthCodeId AuthCodeId,
    DateTimeOffset UsedAt) : DomainEvent;

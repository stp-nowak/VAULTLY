using Vaultly.Core;

namespace Vaultly.Identity.Domain.Sessions.RefreshTokens.Events;

/// <summary>
/// Raised when a refresh token is rotated.
/// </summary>
public sealed record SessionRefreshTokenRotatedDomainEvent(
    SessionId SessionId,
    RefreshTokenId PreviousRefreshTokenId,
    RefreshTokenId RefreshTokenId,
    DateTimeOffset ExpiresAt) : DomainEvent;

using Vaultly.Core;

namespace Vaultly.Identity.Domain.Sessions.RefreshTokens.Events;

/// <summary>
/// Raised when a refresh token is issued for a session.
/// </summary>
public sealed record RefreshTokenIssued(
    SessionId SessionId,
    RefreshTokenId RefreshTokenId,
    DateTimeOffset ExpiresAt) : DomainEvent;

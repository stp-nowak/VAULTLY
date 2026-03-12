using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.SharedKernel;

namespace Vaultly.Identity.Domain.Events;

/// <summary>
/// Raised when a new user aggregate is registered.
/// </summary>
public sealed record UserRegisteredDomainEvent(
    UserId UserId,
    EmailAddress Email,
    PersonName Name,
    bool EmailVerified) : DomainEvent;

/// <summary>
/// Raised when a user profile is updated from an external identity.
/// </summary>
public sealed record UserProfileUpdatedDomainEvent(
    UserId UserId,
    PersonName Name,
    bool EmailVerified) : DomainEvent;

/// <summary>
/// Raised when a user links an external identity.
/// </summary>
public sealed record ExternalIdentityLinkedDomainEvent(
    UserId UserId,
    ExternalIdentityId ExternalIdentityId,
    ProviderId ProviderId,
    ProviderUserId ProviderUserId) : DomainEvent;

/// <summary>
/// Raised when a new session starts for a user.
/// </summary>
public sealed record SessionStartedDomainEvent(
    SessionId SessionId,
    UserId UserId,
    AuthMethod AuthMethod) : DomainEvent;

/// <summary>
/// Raised when a session is revoked.
/// </summary>
public sealed record SessionRevokedDomainEvent(
    SessionId SessionId,
    UserId UserId) : DomainEvent;

/// <summary>
/// Raised when an auth code is issued for a session.
/// </summary>
public sealed record SessionAuthCodeIssuedDomainEvent(
    SessionId SessionId,
    AuthCodeId AuthCodeId,
    DateTimeOffset ExpiresAt) : DomainEvent;

/// <summary>
/// Raised when an auth code is consumed successfully.
/// </summary>
public sealed record SessionAuthCodeConsumedDomainEvent(
    SessionId SessionId,
    AuthCodeId AuthCodeId,
    DateTimeOffset UsedAt) : DomainEvent;

/// <summary>
/// Raised when a refresh token is issued for a session.
/// </summary>
public sealed record SessionRefreshTokenIssuedDomainEvent(
    SessionId SessionId,
    RefreshTokenId RefreshTokenId,
    DateTimeOffset ExpiresAt) : DomainEvent;

/// <summary>
/// Raised when a refresh token is rotated.
/// </summary>
public sealed record SessionRefreshTokenRotatedDomainEvent(
    SessionId SessionId,
    RefreshTokenId PreviousRefreshTokenId,
    RefreshTokenId RefreshTokenId,
    DateTimeOffset ExpiresAt) : DomainEvent;

/// <summary>
/// Raised when a new OAuth state is created for an authorization flow.
/// </summary>
public sealed record OAuthStateCreatedDomainEvent(
    OAuthStateId OAuthStateId,
    OAuthStateValue State,
    DateTimeOffset ExpiresAt) : DomainEvent;

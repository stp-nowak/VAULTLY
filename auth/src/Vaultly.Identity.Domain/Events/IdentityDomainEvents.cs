using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.SharedKernel;

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
/// Raised when a user gets a password (registration excluded, see <see cref="UserRegisteredDomainEvent"/>).
/// </summary>
public sealed record UserPasswordSetDomainEvent(UserId UserId) : DomainEvent;

/// <summary>
/// Raised when a user's password is removed, e.g. because a provider verified the email of an unverified account.
/// </summary>
public sealed record UserPasswordRemovedDomainEvent(UserId UserId) : DomainEvent;

/// <summary>
/// Raised when too many failed password sign-ins pause password sign-in for a user.
/// </summary>
public sealed record UserLockedOutDomainEvent(UserId UserId, DateTimeOffset LockedUntil) : DomainEvent;

/// <summary>
/// Raised when a user gets a different role.
/// </summary>
public sealed record UserRoleChangedDomainEvent(
    UserId UserId,
    UserRole PreviousRole,
    UserRole Role) : DomainEvent;

/// <summary>
/// Raised when an admin switches a sign-in method on.
/// </summary>
public sealed record SignInMethodEnabledDomainEvent(SignInMethod Method) : DomainEvent;

/// <summary>
/// Raised when an admin switches a sign-in method off.
/// </summary>
public sealed record SignInMethodDisabledDomainEvent(SignInMethod Method) : DomainEvent;

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

/// <summary>
/// Raised when a client starts signing a user in.
/// </summary>
public sealed record AuthorizationRequestCreatedDomainEvent(
    AuthorizationRequestId AuthorizationRequestId,
    ClientId ClientId,
    DateTimeOffset ExpiresAt) : DomainEvent;

/// <summary>
/// Raised when an authorization request produced an authorization code.
/// </summary>
public sealed record AuthorizationRequestCompletedDomainEvent(
    AuthorizationRequestId AuthorizationRequestId,
    ClientId ClientId,
    DateTimeOffset CompletedAt) : DomainEvent;

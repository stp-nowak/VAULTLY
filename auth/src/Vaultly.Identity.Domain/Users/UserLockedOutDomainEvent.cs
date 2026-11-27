using Vaultly.Core;

namespace Vaultly.Identity.Domain.Users;

/// <summary>
/// Raised when too many failed password sign-ins pause password sign-in for a user.
/// </summary>
public sealed record UserLockedOutDomainEvent(UserId UserId, DateTimeOffset LockedUntil) : DomainEvent;

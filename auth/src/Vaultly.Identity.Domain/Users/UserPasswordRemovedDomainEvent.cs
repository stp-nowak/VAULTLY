using Vaultly.Core;

namespace Vaultly.Identity.Domain.Users;

/// <summary>
/// Raised when a user's password is removed, e.g. because a provider verified the email of an unverified account.
/// </summary>
public sealed record UserPasswordRemovedDomainEvent(UserId UserId) : DomainEvent;

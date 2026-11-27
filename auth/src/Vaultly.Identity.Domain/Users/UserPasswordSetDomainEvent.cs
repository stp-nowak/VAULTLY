using Vaultly.Core;

namespace Vaultly.Identity.Domain.Users;

/// <summary>
/// Raised when a user gets a password (registration excluded, see <see cref="UserRegisteredDomainEvent"/>).
/// </summary>
public sealed record UserPasswordSetDomainEvent(UserId UserId) : DomainEvent;

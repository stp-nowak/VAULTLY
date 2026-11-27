using Vaultly.Core;

namespace Vaultly.Identity.Domain.Users.Passwords.Events;

/// <summary>
/// Raised when a user gets a password (registration excluded, see <see cref="UserRegistered"/>).
/// </summary>
public sealed record UserPasswordSet(UserId UserId) : DomainEvent;

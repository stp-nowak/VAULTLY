using Vaultly.Core;

namespace Vaultly.Identity.Domain.Users.Events;

/// <summary>
/// Raised when a new user aggregate is registered.
/// </summary>
public sealed record UserRegistered(
    UserId UserId,
    EmailAddress Email,
    PersonName Name,
    bool EmailVerified) : DomainEvent;

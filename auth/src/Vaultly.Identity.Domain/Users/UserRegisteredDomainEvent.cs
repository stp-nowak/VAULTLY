using Vaultly.Core;

namespace Vaultly.Identity.Domain.Users;

/// <summary>
/// Raised when a new user aggregate is registered.
/// </summary>
public sealed record UserRegisteredDomainEvent(
    UserId UserId,
    EmailAddress Email,
    PersonName Name,
    bool EmailVerified) : DomainEvent;

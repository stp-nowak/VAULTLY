using Vaultly.Core;

namespace Vaultly.Identity.Domain.Users.Events;

/// <summary>
/// Raised when a user profile is updated from an external identity.
/// </summary>
public sealed record UserProfileUpdated(
    UserId UserId,
    PersonName Name,
    bool EmailVerified) : DomainEvent;

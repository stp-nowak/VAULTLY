using Vaultly.Core;
using Vaultly.Identity.PublicTypes;

namespace Vaultly.Identity.Domain.Users;

/// <summary>
/// Raised when a user gets a different role.
/// </summary>
public sealed record UserRoleChangedDomainEvent(
    UserId UserId,
    UserRole PreviousRole,
    UserRole Role) : DomainEvent;

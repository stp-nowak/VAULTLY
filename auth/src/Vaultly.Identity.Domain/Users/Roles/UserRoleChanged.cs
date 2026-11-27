using Vaultly.Core;
using Vaultly.Identity.PublicTypes;

namespace Vaultly.Identity.Domain.Users.Roles;

/// <summary>
/// Raised when a user gets a different role.
/// </summary>
public sealed record UserRoleChanged(
    UserId UserId,
    UserRole PreviousRole,
    UserRole Role) : DomainEvent;

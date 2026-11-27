using Vaultly.Identity.Application.Messaging;
using Vaultly.Identity.PublicTypes;

namespace Vaultly.Identity.Application.Features.Users.AssignUserRole;

/// <summary>
/// An admin gives a user another role. Returns <c>false</c> when the user does not exist.
/// </summary>
/// <param name="AdminUserId">The admin making the change, for the audit log.</param>
public sealed record AssignUserRoleCommand(Guid AdminUserId, Guid UserId, UserRole Role) : ICommand<bool>;

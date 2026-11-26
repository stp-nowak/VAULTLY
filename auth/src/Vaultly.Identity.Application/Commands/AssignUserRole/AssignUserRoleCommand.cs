using Vaultly.Identity.Application.Messaging;

namespace Vaultly.Identity.Application.Commands.AssignUserRole;

/// <summary>
/// An admin gives a user another role. Returns <c>false</c> when the user does not exist.
/// </summary>
/// <param name="AdminUserId">The admin making the change, for the audit log.</param>
/// <param name="Role">Role name, e.g. <c>admin</c> or <c>user</c>.</param>
public sealed record AssignUserRoleCommand(Guid AdminUserId, Guid UserId, string Role) : ICommand<bool>;

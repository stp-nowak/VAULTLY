using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.PublicTypes;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Domain.Services;

/// <summary>
/// Assigns roles while keeping the service manageable: the last admin can never lose the role.
/// </summary>
public static class RoleAssignment
{
    /// <param name="user">User whose role changes.</param>
    /// <param name="role">New role.</param>
    /// <param name="adminCount">How many admins exist right now, including <paramref name="user"/> if it is one.</param>
    /// <exception cref="LastAdminException">The user is the only admin and would lose the role.</exception>
    public static void Assign(User user, UserRole role, int adminCount)
    {
        if (user.IsAdmin && role != UserRole.Admin && adminCount <= 1)
        {
            throw new LastAdminException();
        }

        user.ChangeRole(role);
    }
}

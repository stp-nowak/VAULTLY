namespace Vaultly.Identity.Domain.Enums;

/// <summary>
/// What a user may do in the Identity service: every user signs in to client apps; an admin also manages users
/// and settings in the admin console.
/// </summary>
public enum UserRole
{
    User,
    Admin,
}

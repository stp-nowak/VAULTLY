using System.Text.Json;

namespace Vaultly.Identity.PublicTypes;

/// <summary>
/// Claims of the Identity service's access tokens, and how their values are spelled.
/// </summary>
public static class IdentityClaims
{
    public const string UserId = "sub";
    public const string SessionId = "sid";
    public const string AuthMethod = "amr";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";

    /// <summary>The <see cref="Role"/> claim value: the member name in <c>snake_case</c> (<c>admin</c>).</summary>
    public static string ValueOf(UserRole role) => JsonNamingPolicy.SnakeCaseLower.ConvertName(role.ToString());

    /// <summary>Reads a <see cref="Role"/> claim value; <c>false</c> for an unknown or missing role.</summary>
    public static bool TryParseRole(string? value, out UserRole role)
    {
        foreach (var candidate in Enum.GetValues<UserRole>())
        {
            if (ValueOf(candidate) == value)
            {
                role = candidate;
                return true;
            }
        }

        role = default;
        return false;
    }
}

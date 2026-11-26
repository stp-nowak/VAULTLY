using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

/// <summary>
/// What a user may do in the Identity service: every user signs in to client apps; an admin also manages users
/// and settings in the admin console.
/// </summary>
public sealed record UserRole
{
    public const int MaxLength = 20;

    public static readonly UserRole User = new("user");
    public static readonly UserRole Admin = new("admin");

    private static readonly IReadOnlyDictionary<string, UserRole> Known = new Dictionary<string, UserRole>
    {
        [User.Value] = User,
        [Admin.Value] = Admin,
    };

    private UserRole(string value)
    {
        Value = value;
    }

    public string Value { get; }

    /// <summary>Returns the role with this name (case-insensitive).</summary>
    /// <exception cref="InvalidValueException">The name is not a known role.</exception>
    public static UserRole Parse(string? value)
        => Known.TryGetValue(value?.Trim().ToLowerInvariant() ?? string.Empty, out var role)
            ? role
            : throw new InvalidValueException("Unknown user role.");

    public override string ToString() => Value;
}

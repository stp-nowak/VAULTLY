using System.Text.Json;

namespace Vaultly.Identity.Application.Common;

/// <summary>
/// How enums are spelled outside the code — JSON, database columns, token claims, query strings: <c>snake_case</c> of
/// the member name (<c>UserRole.Admin</c> → <c>admin</c>, <c>OAuthError.InvalidGrant</c> → <c>invalid_grant</c>).
/// Derived from the members, so no option is ever written as a string by hand.
/// </summary>
public static class WireNames
{
    public static JsonNamingPolicy Policy => JsonNamingPolicy.SnakeCaseLower;

    public static string Of<TEnum>(TEnum value) where TEnum : struct, Enum => Policy.ConvertName(value.ToString());

    /// <summary>Reads an <see cref="Of{TEnum}"/> spelling back; throws for anything else.</summary>
    public static TEnum Parse<TEnum>(string value) where TEnum : struct, Enum
        => TryParse<TEnum>(value, out var member) ? member : throw new ArgumentException($"Unknown {typeof(TEnum).Name}.", nameof(value));

    /// <summary>Reads an <see cref="Of{TEnum}"/> spelling back; <c>false</c> for anything else (numbers included).</summary>
    public static bool TryParse<TEnum>(string? value, out TEnum member) where TEnum : struct, Enum
    {
        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (Of(candidate) == value)
            {
                member = candidate;
                return true;
            }
        }

        member = default;
        return false;
    }

    /// <summary>Every member's spelling, e.g. for a discovery document.</summary>
    public static string[] All<TEnum>() where TEnum : struct, Enum => [.. Enum.GetValues<TEnum>().Select(Of)];
}

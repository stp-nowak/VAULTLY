using System.Text.Json;

namespace Vaultly.Identity.ApiContracts;

/// <summary>
/// How the Identity API spells enum values in forms and JSON: the member name in <c>snake_case</c>
/// (<see cref="GrantType.AuthorizationCode"/> → <c>authorization_code</c>).
/// </summary>
public static class WireName
{
    public static JsonNamingPolicy Policy => JsonNamingPolicy.SnakeCaseLower;

    public static string Of<TEnum>(TEnum value) where TEnum : struct, Enum => Policy.ConvertName(value.ToString());
}

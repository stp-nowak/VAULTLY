using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Vaultly.Identity.Api;

public static class ClaimsExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static Guid? GetSessionId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("sid");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
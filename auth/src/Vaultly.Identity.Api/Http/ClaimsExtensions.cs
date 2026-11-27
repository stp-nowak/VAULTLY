using System.Security.Claims;

using Vaultly.Identity.PublicTypes;

namespace Vaultly.Identity.Api.Http;

public static class ClaimsExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(IdentityClaims.UserId);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static Guid? GetSessionId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(IdentityClaims.SessionId);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
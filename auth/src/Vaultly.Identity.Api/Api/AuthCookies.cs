using Microsoft.AspNetCore.Http;

namespace Vaultly.Identity.Api;

public static class AuthCookies
{
    public const string RefreshTokenCookieName = "vaultly_refresh";

    public static void SetRefreshToken(HttpResponse response, string refreshToken, DateTimeOffset expiresAt, string? domain)
    {
        var options = BuildOptions(expiresAt, domain);
        response.Cookies.Append(RefreshTokenCookieName, refreshToken, options);
    }

    public static bool TryGetRefreshToken(HttpRequest request, out string refreshToken)
    {
        if (request.Cookies.TryGetValue(RefreshTokenCookieName, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            refreshToken = value;
            return true;
        }

        refreshToken = string.Empty;
        return false;
    }

    public static void ClearRefreshToken(HttpResponse response, string? domain)
    {
        var options = BuildOptions(DateTimeOffset.UtcNow.AddDays(-1), domain);
        response.Cookies.Append(RefreshTokenCookieName, string.Empty, options);
    }

    private static CookieOptions BuildOptions(DateTimeOffset expiresAt, string? domain)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Expires = expiresAt.UtcDateTime
        };

        if (!string.IsNullOrWhiteSpace(domain))
        {
            options.Domain = domain;
        }

        return options;
    }
}
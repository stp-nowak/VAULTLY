namespace Vaultly.Identity.Api.Http;

/// <summary>
/// The single sign-on cookie of the auth service. It identifies the browser's SSO session so a later
/// <c>/authorize</c> can sign the user in without showing the login page again.
/// </summary>
public static class SsoCookie
{
    public const string Name = "vaultly_sso";

    /// <summary>
    /// Writes the cookie. SameSite=Lax is required: <c>/authorize</c> is reached by a top-level navigation from the
    /// client application, and Strict cookies would not be sent with it.
    /// </summary>
    public static void Set(HttpResponse response, string ssoToken, DateTimeOffset expiresAt)
        => response.Cookies.Append(Name, ssoToken, BuildOptions(expiresAt));

    /// <summary>Reads the SSO token, if present.</summary>
    public static string? Read(HttpRequest request)
        => request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    /// <summary>Deletes the cookie.</summary>
    public static void Clear(HttpResponse response) => response.Cookies.Delete(Name, BuildOptions(expiresAt: null));

    private static CookieOptions BuildOptions(DateTimeOffset? expiresAt)
        => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true,
            Expires = expiresAt,
        };
}

using System.ComponentModel.DataAnnotations;

namespace Vaultly.Web.Api.Features.Auth;

/// <summary>
/// Configuration of the Auth feature (section <c>Auth</c>).
/// </summary>
public sealed class AuthOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Auth";

    /// <summary>Base URL of the Identity service (<c>./auth</c>), reachable from the web API.</summary>
    [Required, Url]
    public string IdentityBaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Origin under which the browser reaches the web API (same origin as the SPA). Used to build the OAuth callback
    /// URL, which must be allowed in the Identity service <c>AllowedRedirectUris</c>.
    /// </summary>
    [Required, Url]
    public string PublicOrigin { get; init; } = string.Empty;

    /// <summary>Name of the HttpOnly session cookie. Use the <c>__Host-</c> prefix outside local development.</summary>
    [Required]
    public string SessionCookieName { get; init; } = "__Host-vaultly-session";

    /// <summary>Name of the short-lived cookie that correlates login start with the OAuth callback.</summary>
    [Required]
    public string LoginCookieName { get; init; } = "__Host-vaultly-login";

    /// <summary>How long a started login may take before the callback is rejected.</summary>
    [Range(1, 60)]
    public int LoginTimeoutMinutes { get; init; } = 10;

    /// <summary>Access tokens expiring within this window are refreshed before use.</summary>
    [Range(0, 600)]
    public int AccessTokenRefreshSkewSeconds { get; init; } = 60;

    /// <summary>SPA path used after login when no valid local return URL was provided.</summary>
    [Required]
    public string DefaultReturnPath { get; init; } = "/app";

    /// <summary>SPA login page; failed callbacks redirect here with an <c>error</c> query parameter.</summary>
    [Required]
    public string LoginPagePath { get; init; } = "/login";
}

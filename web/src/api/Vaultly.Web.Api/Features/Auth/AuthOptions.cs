using System.ComponentModel.DataAnnotations;

namespace Vaultly.Web.Api.Features.Auth;

/// <summary>
/// Configuration of the Auth feature (section <c>Auth</c>). The web API is a confidential OIDC client of the
/// Identity service.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    [Required, Url]
    public string IdentityBaseUrl { get; init; } = string.Empty;

    [Required]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Provide through the environment/secret store (<c>Auth__ClientSecret</c>), never in committed config.</summary>
    [Required]
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Origin under which the browser reaches this API (same origin as the SPA). Callback and post-logout URLs built
    /// from it must be registered for the client in the Identity service.
    /// </summary>
    [Required, Url]
    public string PublicOrigin { get; init; } = string.Empty;

    /// <summary>Use the <c>__Host-</c> prefix outside local development.</summary>
    [Required]
    public string SessionCookieName { get; init; } = "__Host-vaultly-session";

    [Required]
    public string LoginCookieName { get; init; } = "__Host-vaultly-login";

    [Range(1, 60)]
    public int LoginTimeoutMinutes { get; init; } = 10;

    [Range(0, 600)]
    public int AccessTokenRefreshSkewSeconds { get; init; } = 60;

    [Required]
    public string DefaultReturnPath { get; init; } = "/app";

    /// <summary>SPA page that shows sign-in errors (<c>?error=</c>).</summary>
    [Required]
    public string LoginPagePath { get; init; } = "/login";

    [Required]
    public string PostLogoutRedirectPath { get; init; } = "/";
}

using System.Text.Json;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Vaultly.Web.Api.Features.Auth.Sessions;

/// <summary>
/// The only credential the browser holds: an opaque session id in an HttpOnly cookie.
/// </summary>
public sealed class SessionCookie(IOptions<AuthOptions> options)
{
    private readonly AuthOptions _options = options.Value;

    /// <summary>Reads the session id from the request, if present.</summary>
    public string? Read(HttpRequest request)
        => request.Cookies.TryGetValue(_options.SessionCookieName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;

    /// <summary>
    /// Writes the session cookie. SameSite=Strict is safe because the SPA calls the web API same-origin and the cookie
    /// is never needed on cross-site navigations (the OAuth callback sets it, it does not read it).
    /// </summary>
    public void Append(HttpResponse response, string sessionId, DateTimeOffset expiresAt)
        => response.Cookies.Append(_options.SessionCookieName, sessionId, Build(SameSiteMode.Strict, expiresAt));

    /// <summary>Removes the session cookie.</summary>
    public void Delete(HttpResponse response)
        => response.Cookies.Delete(_options.SessionCookieName, Build(SameSiteMode.Strict, expiresAt: null));

    /// <summary>
    /// Shared cookie flags. Path "/" and no Domain keep the cookie compatible with the <c>__Host-</c> prefix.
    /// </summary>
    internal static CookieOptions Build(SameSiteMode sameSite, DateTimeOffset? expiresAt)
        => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = sameSite,
            Path = "/",
            IsEssential = true,
            Expires = expiresAt,
        };
}

/// <summary>
/// Data correlating a login start with its OAuth callback.
/// </summary>
public sealed record LoginState(string State, string ReturnUrl, DateTimeOffset ExpiresAt);

/// <summary>
/// Short-lived, encrypted cookie holding <see cref="LoginState"/>. Binding the callback's <c>state</c> to this cookie
/// prevents login CSRF (an attacker cannot make the victim finish a login the attacker started).
/// </summary>
public sealed class LoginStateCookie(
    IOptions<AuthOptions> options,
    IDataProtectionProvider dataProtectionProvider,
    TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AuthOptions _options = options.Value;
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("Vaultly.Web.Api.LoginState.v1");
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>
    /// Stores the login state. SameSite=Lax is required: the callback is a top-level navigation coming back from
    /// the Identity service/Google, and Strict cookies would not be sent with it.
    /// </summary>
    public void Append(HttpResponse response, LoginState state)
    {
        var payload = _protector.Protect(JsonSerializer.Serialize(state, JsonOptions));
        response.Cookies.Append(_options.LoginCookieName, payload, SessionCookie.Build(SameSiteMode.Lax, state.ExpiresAt));
    }

    /// <summary>
    /// Reads the login state once and deletes the cookie. Returns <c>null</c> when missing, tampered or expired.
    /// </summary>
    public LoginState? ReadAndDelete(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(_options.LoginCookieName, out var payload) || string.IsNullOrEmpty(payload))
        {
            return null;
        }

        context.Response.Cookies.Delete(_options.LoginCookieName, SessionCookie.Build(SameSiteMode.Lax, expiresAt: null));

        try
        {
            var state = JsonSerializer.Deserialize<LoginState>(_protector.Unprotect(payload), JsonOptions);
            return state is not null && state.ExpiresAt > _timeProvider.GetUtcNow() ? state : null;
        }
        catch (Exception exception) when (exception is System.Security.Cryptography.CryptographicException or JsonException)
        {
            return null;
        }
    }
}

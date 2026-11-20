using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

using Vaultly.Web.Api.Common.Csrf;
using Vaultly.Web.Api.Features.Auth.IdentityService;
using Vaultly.Web.Api.Features.Auth.Sessions;

namespace Vaultly.Web.Api.Features.Auth;

/// <summary>
/// HTTP surface of the Auth feature. The SPA only ever sees the session cookie; tokens stay on the server.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>Route prefix of the Auth feature.</summary>
    public const string RoutePrefix = "/api/auth";

    /// <summary>Path of the OAuth callback, relative to the public origin.</summary>
    public const string CallbackPath = RoutePrefix + "/callback";

    /// <summary>
    /// Maps login, callback, current user, session management and logout endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(RoutePrefix).WithTags("Auth");

        group.MapGet("/login", Login).AllowWithoutCsrfHeader();
        group.MapGet("/callback", CallbackAsync).AllowWithoutCsrfHeader();
        group.MapGet("/user", GetUserAsync);
        group.MapGet("/sessions", GetSessionsAsync);
        group.MapDelete("/sessions/{id:guid}", RevokeSessionAsync);
        group.MapPost("/logout", LogoutAsync);

        return endpoints;
    }

    /// <summary>
    /// Starts login: remembers state + return URL in an encrypted cookie and redirects to the Identity service.
    /// The state travels inside our callback URL because the Identity service only appends <c>code</c>.
    /// </summary>
    internal static IResult Login(
        string? returnUrl,
        HttpContext context,
        LoginStateCookie loginStateCookie,
        IOptions<AuthOptions> options,
        TimeProvider timeProvider)
    {
        var authOptions = options.Value;
        var state = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var expiresAt = timeProvider.GetUtcNow().AddMinutes(authOptions.LoginTimeoutMinutes);

        loginStateCookie.Append(
            context.Response,
            new LoginState(state, SanitizeReturnUrl(returnUrl, authOptions.DefaultReturnPath), expiresAt));

        var callbackUrl = QueryHelpers.AddQueryString($"{authOptions.PublicOrigin.TrimEnd('/')}{CallbackPath}", "state", state);
        var identityLoginUrl = QueryHelpers.AddQueryString($"{authOptions.IdentityBaseUrl.TrimEnd('/')}/login", "redirectUri", callbackUrl);

        return Results.Redirect(identityLoginUrl);
    }

    /// <summary>
    /// Completes login: validates state, exchanges the code server-to-server, creates the session and redirects
    /// back into the SPA. Failures redirect to the SPA login page with a non-sensitive error code.
    /// </summary>
    internal static async Task<IResult> CallbackAsync(
        string? code,
        string? state,
        string? error,
        HttpContext context,
        LoginStateCookie loginStateCookie,
        IIdentityServiceClient identity,
        IUserSessionService sessions,
        IOptions<AuthOptions> options,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(typeof(AuthEndpoints));
        var loginPath = options.Value.LoginPagePath;
        var loginState = loginStateCookie.ReadAndDelete(context);

        if (!string.IsNullOrEmpty(error))
        {
            logger.LogWarning("Identity service returned login error {Error}", error);
            return Results.Redirect(QueryHelpers.AddQueryString(loginPath, "error", "login_failed"));
        }

        if (loginState is null || string.IsNullOrEmpty(state) || !FixedTimeEquals(state, loginState.State))
        {
            logger.LogWarning("Rejected OAuth callback with missing, expired or mismatched state");
            return Results.Redirect(QueryHelpers.AddQueryString(loginPath, "error", "invalid_state"));
        }

        if (string.IsNullOrEmpty(code))
        {
            return Results.Redirect(QueryHelpers.AddQueryString(loginPath, "error", "login_failed"));
        }

        try
        {
            var tokens = await identity.ExchangeCodeAsync(code, cancellationToken);
            await sessions.SignInAsync(context, tokens, cancellationToken);
        }
        catch (Exception exception) when (exception is IdentityServiceException or HttpRequestException)
        {
            logger.LogWarning(exception, "Authorization code exchange failed");
            return Results.Redirect(QueryHelpers.AddQueryString(loginPath, "error", "login_failed"));
        }

        return Results.LocalRedirect(loginState.ReturnUrl);
    }

    /// <summary>
    /// Returns the signed-in user, or 401. The SPA uses it to bootstrap its auth state.
    /// </summary>
    internal static async Task<IResult> GetUserAsync(
        HttpContext context,
        IUserSessionService sessions,
        CancellationToken cancellationToken)
    {
        var session = await sessions.GetActiveSessionAsync(context, cancellationToken);
        return session is null
            ? Results.Unauthorized()
            : Results.Ok(new { user = session.User, sessionId = session.AuthSessionId });
    }

    /// <summary>
    /// Lists the user's Identity sessions using the server-side access token.
    /// </summary>
    internal static async Task<IResult> GetSessionsAsync(
        HttpContext context,
        IUserSessionService sessions,
        IIdentityServiceClient identity,
        CancellationToken cancellationToken)
    {
        var session = await sessions.GetActiveSessionAsync(context, cancellationToken);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var items = await identity.GetSessionsAsync(session.AccessToken, cancellationToken);
            return Results.Ok(new { sessions = items });
        }
        catch (IdentityServiceException exception) when (exception.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await sessions.ForgetAsync(context, cancellationToken);
            return Results.Unauthorized();
        }
    }

    /// <summary>
    /// Revokes an Identity session. Revoking the current one also ends the browser session.
    /// </summary>
    internal static async Task<IResult> RevokeSessionAsync(
        Guid id,
        HttpContext context,
        IUserSessionService sessions,
        IIdentityServiceClient identity,
        CancellationToken cancellationToken)
    {
        var session = await sessions.GetActiveSessionAsync(context, cancellationToken);
        if (session is null)
        {
            return Results.Unauthorized();
        }

        if (!await identity.RevokeSessionAsync(session.AccessToken, id, cancellationToken))
        {
            return Results.NotFound();
        }

        if (id == session.AuthSessionId)
        {
            await sessions.ForgetAsync(context, cancellationToken);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Signs out locally and at the Identity service. Idempotent.
    /// </summary>
    internal static async Task<IResult> LogoutAsync(
        HttpContext context,
        IUserSessionService sessions,
        CancellationToken cancellationToken)
    {
        await sessions.SignOutAsync(context, cancellationToken);
        return Results.NoContent();
    }

    /// <summary>
    /// Accepts only local, app-relative paths to prevent open redirects (<c>//evil.com</c>, <c>/\evil.com</c>, absolute URLs).
    /// </summary>
    internal static string SanitizeReturnUrl(string? returnUrl, string fallback)
    {
        if (string.IsNullOrEmpty(returnUrl) || returnUrl[0] != '/')
        {
            return fallback;
        }

        if (returnUrl.Length > 1 && (returnUrl[1] == '/' || returnUrl[1] == '\\'))
        {
            return fallback;
        }

        return returnUrl.StartsWith(RoutePrefix, StringComparison.OrdinalIgnoreCase) ? fallback : returnUrl;
    }

    /// <summary>
    /// Constant-time comparison so the state check does not leak timing information.
    /// </summary>
    private static bool FixedTimeEquals(string left, string right)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}

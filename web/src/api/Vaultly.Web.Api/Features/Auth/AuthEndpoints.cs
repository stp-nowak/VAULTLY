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
    public const string RoutePrefix = "/api/auth";

    public const string CallbackPath = RoutePrefix + "/callback";

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

    internal static IResult Login(
        string? returnUrl,
        HttpContext context,
        LoginStateCookie loginStateCookie,
        IOptions<AuthOptions> options,
        TimeProvider timeProvider)
    {
        var authOptions = options.Value;
        var state = RandomToken();
        var codeVerifier = RandomToken();

        loginStateCookie.Append(
            context.Response,
            new LoginState(
                state,
                codeVerifier,
                SanitizeReturnUrl(returnUrl, authOptions.DefaultReturnPath),
                timeProvider.GetUtcNow().AddMinutes(authOptions.LoginTimeoutMinutes)));

        var authorizeUrl = QueryHelpers.AddQueryString($"{authOptions.IdentityBaseUrl.TrimEnd('/')}/authorize", new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = authOptions.ClientId,
            ["redirect_uri"] = CallbackUrl(authOptions),
            ["state"] = state,
            ["code_challenge"] = CreateCodeChallenge(codeVerifier),
            ["code_challenge_method"] = "S256",
        });

        return Results.Redirect(authorizeUrl);
    }

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
        var authOptions = options.Value;
        var loginState = loginStateCookie.ReadAndDelete(context);

        if (!string.IsNullOrEmpty(error))
        {
            logger.LogWarning("Identity service returned authorization error {Error}", error);
            return LoginError(authOptions, "login_failed");
        }

        if (loginState is null || string.IsNullOrEmpty(state) || !FixedTimeEquals(state, loginState.State))
        {
            logger.LogWarning("Rejected OAuth callback with missing, expired or mismatched state");
            return LoginError(authOptions, "invalid_state");
        }

        if (string.IsNullOrEmpty(code))
        {
            return LoginError(authOptions, "login_failed");
        }

        try
        {
            var tokens = await identity.ExchangeCodeAsync(code, loginState.CodeVerifier, CallbackUrl(authOptions), cancellationToken);
            await sessions.SignInAsync(context, tokens, cancellationToken);
        }
        catch (Exception exception) when (exception is IdentityServiceException or HttpRequestException)
        {
            logger.LogWarning(exception, "Authorization code exchange failed");
            return LoginError(authOptions, "login_failed");
        }

        return Results.LocalRedirect(loginState.ReturnUrl);
    }

    internal static async Task<IResult> GetUserAsync(HttpContext context, IUserSessionService sessions, CancellationToken cancellationToken)
    {
        var session = await sessions.GetActiveSessionAsync(context, cancellationToken);
        return session is null
            ? Results.Unauthorized()
            : Results.Ok(new { user = session.User, sessionId = session.AuthSessionId });
    }

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
            return Results.Ok(new { sessions = await identity.GetSessionsAsync(session.AccessToken, cancellationToken) });
        }
        catch (IdentityServiceException exception) when (exception.StatusCode == StatusCodes.Status401Unauthorized)
        {
            await sessions.ForgetAsync(context, cancellationToken);
            return Results.Unauthorized();
        }
    }

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

    internal static async Task<IResult> LogoutAsync(
        HttpContext context,
        IUserSessionService sessions,
        IOptions<AuthOptions> options,
        CancellationToken cancellationToken)
    {
        await sessions.SignOutAsync(context, cancellationToken);

        var authOptions = options.Value;
        var logoutUrl = QueryHelpers.AddQueryString($"{authOptions.IdentityBaseUrl.TrimEnd('/')}/logout", new Dictionary<string, string?>
        {
            ["client_id"] = authOptions.ClientId,
            ["post_logout_redirect_uri"] = $"{authOptions.PublicOrigin.TrimEnd('/')}{authOptions.PostLogoutRedirectPath}",
        });

        return Results.Ok(new { logoutUrl });
    }

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

    internal static string CreateCodeChallenge(string codeVerifier)
        => WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

    private static string CallbackUrl(AuthOptions options) => $"{options.PublicOrigin.TrimEnd('/')}{CallbackPath}";

    private static string RandomToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static IResult LoginError(AuthOptions options, string error)
        => Results.Redirect(QueryHelpers.AddQueryString(options.LoginPagePath, "error", error));

    private static bool FixedTimeEquals(string left, string right)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}

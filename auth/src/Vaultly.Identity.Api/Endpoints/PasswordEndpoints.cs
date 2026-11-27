using System.Threading.RateLimiting;

using MediatR;

using Vaultly.Identity.Api.Contracts;
using Vaultly.Identity.Api.Http;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Features.Passwords.RegisterWithPassword;
using Vaultly.Identity.Application.Features.Passwords.SignInWithPassword;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Domain;
using Vaultly.Identity.Domain.AuthorizationRequests.Exceptions;
using Vaultly.Identity.Domain.Users.Passwords;

namespace Vaultly.Identity.Api.Endpoints;

/// <summary>
/// Email and password sign-in and registration for the login UI. Both continue a pending authorization request; on
/// success they set the SSO cookie and return where the browser goes next.
/// </summary>
public static class PasswordEndpoints
{
    public const string RateLimitPolicy = "password";

    /// <summary>Attempts per client address per minute, on top of the per-account lockout.</summary>
    public const int AttemptsPerMinute = 20;

    public static IServiceCollection AddPasswordRateLimiting(this IServiceCollection services)
        => services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = AttemptsPerMinute, Window = TimeSpan.FromMinutes(1) }));
        });

    /// <summary>
    /// Maps <c>POST /api/authorization-requests/{id}/password-sign-in</c> and <c>…/registration</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapPasswordEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/authorization-requests/{id:guid}")
            .AddEndpointFilter(CsrfHeaderFilter.Require)
            .RequireRateLimiting(RateLimitPolicy);

        api.MapPost("/password-sign-in", SignInAsync);
        api.MapPost("/registration", RegisterAsync);
        return app;
    }

    internal static Task<IResult> SignInAsync(Guid id, PasswordSignInRequest body, HttpContext context, ISender sender)
        => SignInBrowserAsync(context, () => sender.Send(
            new SignInWithPasswordCommand(id, body.Email ?? string.Empty, body.Password ?? string.Empty, UserAgent(context), IpAddress(context)),
            context.RequestAborted));

    internal static Task<IResult> RegisterAsync(Guid id, RegistrationRequest body, HttpContext context, ISender sender)
        => SignInBrowserAsync(context, () => sender.Send(
            new RegisterWithPasswordCommand(
                id, body.Name ?? string.Empty, body.Email ?? string.Empty, body.Password ?? string.Empty, UserAgent(context), IpAddress(context)),
            context.RequestAborted));

    private static async Task<IResult> SignInBrowserAsync(HttpContext context, Func<Task<AuthCallbackResult>> signIn)
    {
        try
        {
            var result = await signIn();
            SsoCookie.Set(context.Response, result.SsoToken, result.SsoExpiresAt);
            return Results.Ok(new { redirectUrl = result.RedirectUrl });
        }
        catch (PasswordSignInException ex) when (ex.Failure == PasswordSignInFailure.EmailTaken)
        {
            return Results.Conflict(new ApiErrorResponse(ApiError.EmailTaken));
        }
        catch (PasswordSignInException)
        {
            return Results.BadRequest(new ApiErrorResponse(ApiError.InvalidCredentials));
        }
        catch (WeakPasswordException)
        {
            return Results.BadRequest(new ApiErrorResponse(ApiError.WeakPassword));
        }
        catch (Exception ex) when (ex is AuthorizationRequestExpiredException or AuthorizationRequestCompletedException or AuthorizationRequestNotFoundException)
        {
            return Results.BadRequest(new ApiErrorResponse(ApiError.RequestExpired));
        }
        catch (InvalidValueException)
        {
            return Results.BadRequest(new ApiErrorResponse(ApiError.InvalidInput));
        }
        catch (InvalidOperationException)
        {
            return Results.Json(new ApiErrorResponse(ApiError.SignInMethodDisabled), statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private static string UserAgent(HttpContext context) => context.Request.Headers.UserAgent.ToString();

    private static string? IpAddress(HttpContext context) => context.Connection.RemoteIpAddress?.ToString();

    /// <summary>Body of <c>…/password-sign-in</c>.</summary>
    public sealed record PasswordSignInRequest(string? Email, string? Password);

    /// <summary>Body of <c>…/registration</c>.</summary>
    public sealed record RegistrationRequest(string? Name, string? Email, string? Password);
}

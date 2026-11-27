using System.Net.Http.Headers;
using System.Text;

using MediatR;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

using Vaultly.Core;
using Vaultly.Identity.Api.Contracts;
using Vaultly.Identity.Api.Http;
using Vaultly.Identity.ApiContracts;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Features.Authorization.ExchangeAuthCode;
using Vaultly.Identity.Application.Features.Authorization.GetAuthorizationRequest;
using Vaultly.Identity.Application.Features.Authorization.StartAuthorization;
using Vaultly.Identity.Application.Features.ExternalLogin.HandleOAuthCallback;
using Vaultly.Identity.Application.Features.ExternalLogin.StartExternalLogin;
using Vaultly.Identity.Application.Features.Sessions.EndSession;
using Vaultly.Identity.Application.Features.Sessions.RefreshSession;
using Vaultly.Identity.Application.Features.Sessions.RevokeToken;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Domain.AuthorizationRequests;

namespace Vaultly.Identity.Api.Endpoints;

/// <summary>
/// OAuth 2.0 / OIDC endpoints of the authorization server and the JSON API used by the login UI (auth/web).
/// </summary>
public static class OAuthEndpoints
{
    private const string ProviderRoute = "{provider:regex(^[a-z0-9-]+$)}";

    /// <summary>
    /// Maps <c>/authorize</c>, provider login/callback, <c>/token</c>, <c>/revoke</c>, <c>/logout</c> and the login UI API.
    /// </summary>
    public static IEndpointRouteBuilder MapOAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/authorize", AuthorizeAsync);
        app.MapGet($"/{ProviderRoute}/authorize", StartProviderLoginAsync);
        app.MapGet($"/{ProviderRoute}/callback", ProviderCallbackAsync);
        app.MapPost("/token", TokenAsync).DisableAntiforgery();
        app.MapPost("/revoke", RevokeAsync).DisableAntiforgery();
        app.MapGet("/logout", LogoutAsync);
        app.MapGet("/api/authorization-requests/{id:guid}", GetAuthorizationRequestAsync);
        return app;
    }

    internal static async Task<IResult> AuthorizeAsync(HttpContext context, ISender sender, IOptions<IdentityOptions> options, ILoggerFactory loggerFactory)
    {
        var query = context.Request.Query;
        try
        {
            var result = await sender.Send(
                new StartAuthorizationCommand(
                    query["client_id"].ToString(),
                    query["redirect_uri"].ToString(),
                    query["response_type"].ToString(),
                    query["state"].ToString(),
                    query["code_challenge"].ToString(),
                    query["code_challenge_method"].ToString(),
                    SsoCookie.Read(context.Request)),
                context.RequestAborted);

            return Results.Redirect(result.RedirectUrl);
        }
        catch (OAuthProtocolException ex)
        {
            Logger(loggerFactory).LogWarning("Rejected /authorize request: {Error} {Description}", ex.Error, ex.Message);
            return LoginUiError(options, SignInErrorCode.InvalidRequest);
        }
    }

    internal static async Task<IResult> StartProviderLoginAsync(
        string provider,
        Guid? request,
        HttpContext context,
        ISender sender,
        IOptions<IdentityOptions> options,
        ILoggerFactory loggerFactory)
    {
        if (request is null)
        {
            return LoginUiError(options, SignInErrorCode.InvalidRequest);
        }

        try
        {
            var url = await sender.Send(new StartExternalLoginCommand(provider, request.Value), context.RequestAborted);
            return Results.Redirect(url);
        }
        catch (Exception ex) when (ex is DomainException or InvalidOperationException)
        {
            Logger(loggerFactory).LogWarning(ex, "Could not start {Provider} login", provider);
            return LoginUiError(options, ToUiErrorCode(ex));
        }
    }

    internal static async Task<IResult> ProviderCallbackAsync(
        string provider,
        HttpContext context,
        ISender sender,
        IOptions<IdentityOptions> options,
        ILoggerFactory loggerFactory)
    {
        var query = context.Request.Query;
        var error = query["error"].ToString();
        if (!string.IsNullOrEmpty(error))
        {
            Logger(loggerFactory).LogWarning("{Provider} returned error {Error}", provider, error);
            return LoginUiError(options, error == WireNames.Of(OAuthError.AccessDenied) ? SignInErrorCode.AccessDenied : SignInErrorCode.LoginFailed);
        }

        var code = query["code"].ToString();
        var state = query["state"].ToString();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return LoginUiError(options, SignInErrorCode.InvalidRequest);
        }

        try
        {
            var result = await sender.Send(
                new HandleOAuthCallbackCommand(
                    provider,
                    code,
                    state,
                    context.Request.Headers.UserAgent.ToString(),
                    context.Connection.RemoteIpAddress?.ToString()),
                context.RequestAborted);

            SsoCookie.Set(context.Response, result.SsoToken, result.SsoExpiresAt);
            return Results.Redirect(result.RedirectUrl);
        }
        catch (Exception ex) when (ex is DomainException or InvalidOperationException or HttpRequestException)
        {
            Logger(loggerFactory).LogWarning(ex, "{Provider} callback failed", provider);
            return LoginUiError(options, ToUiErrorCode(ex));
        }
    }

    internal static async Task<IResult> TokenAsync(HttpContext context, ISender sender)
    {
        NoStore(context.Response);
        if (!context.Request.HasFormContentType)
        {
            return OAuthErrorResponse(OAuthProtocolException.InvalidRequest("Form-encoded body is required."));
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var (clientId, clientSecret) = ReadClientCredentials(context.Request, form);

        try
        {
            if (!WireNames.TryParse<GrantType>(form[TokenRequestFields.GrantType].ToString(), out var grantType))
            {
                throw OAuthProtocolException.UnsupportedGrantType();
            }

            TokenResult result = grantType switch
            {
                GrantType.AuthorizationCode => await sender.Send(
                    new ExchangeAuthCodeCommand(
                        form[TokenRequestFields.Code],
                        clientId,
                        clientSecret,
                        form[TokenRequestFields.RedirectUri],
                        form[TokenRequestFields.CodeVerifier]),
                    context.RequestAborted),
                GrantType.RefreshToken => await sender.Send(
                    new RefreshSessionCommand(form[TokenRequestFields.RefreshToken], clientId, clientSecret),
                    context.RequestAborted),
                _ => throw OAuthProtocolException.UnsupportedGrantType(),
            };

            return Results.Ok(ToResponse(result));
        }
        catch (OAuthProtocolException ex)
        {
            return OAuthErrorResponse(ex);
        }
    }

    internal static async Task<IResult> RevokeAsync(HttpContext context, ISender sender)
    {
        if (!context.Request.HasFormContentType)
        {
            return OAuthErrorResponse(OAuthProtocolException.InvalidRequest("Form-encoded body is required."));
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        var (clientId, clientSecret) = ReadClientCredentials(context.Request, form);

        try
        {
            await sender.Send(new RevokeTokenCommand(form[TokenRequestFields.Token], clientId, clientSecret), context.RequestAborted);
            return Results.Ok();
        }
        catch (OAuthProtocolException ex)
        {
            return OAuthErrorResponse(ex);
        }
    }

    internal static async Task<IResult> LogoutAsync(HttpContext context, ISender sender)
    {
        var result = await sender.Send(
            new EndSessionCommand(
                SsoCookie.Read(context.Request),
                context.Request.Query["client_id"].ToString(),
                context.Request.Query["post_logout_redirect_uri"].ToString()),
            context.RequestAborted);

        SsoCookie.Clear(context.Response);
        return Results.Redirect(result.RedirectUrl);
    }

    internal static async Task<IResult> GetAuthorizationRequestAsync(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var request = await sender.Send(new GetAuthorizationRequestQuery(id), cancellationToken);
        return request is null ? Results.NotFound() : Results.Ok(request);
    }

    private static (string? ClientId, string? ClientSecret) ReadClientCredentials(HttpRequest request, IFormCollection form)
    {
        if (AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var header)
            && string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            && header.Parameter is { } parameter)
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parameter));
                var separator = decoded.IndexOf(':');
                if (separator > 0)
                {
                    return (Uri.UnescapeDataString(decoded[..separator]), Uri.UnescapeDataString(decoded[(separator + 1)..]));
                }
            }
            catch (FormatException)
            {
                return (null, null);
            }
        }

        return (form[TokenRequestFields.ClientId].ToString(), form[TokenRequestFields.ClientSecret].ToString());
    }

    private static IResult OAuthErrorResponse(OAuthProtocolException ex)
        => Results.Json(new { error = ex.Error, error_description = ex.Message }, statusCode: ex.StatusCode);

    private static void NoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
    }

    private static IResult LoginUiError(IOptions<IdentityOptions> options, SignInErrorCode code)
        => Results.Redirect(QueryHelpers.AddQueryString($"{options.Value.ResolveLoginUiBaseUrl()}/error", "code", WireNames.Of(code)));

    private static SignInErrorCode ToUiErrorCode(Exception ex)
        => ex is AuthorizationRequestExpiredException or AuthorizationRequestCompletedException or AuthorizationRequestNotFoundException
            ? SignInErrorCode.RequestExpired
            : SignInErrorCode.LoginFailed;

    private static ILogger Logger(ILoggerFactory loggerFactory) => loggerFactory.CreateLogger(typeof(OAuthEndpoints));

    private static TokenResponse ToResponse(TokenResult result)
        => new(
            result.AccessToken,
            TokenResponse.BearerTokenType,
            result.ExpiresInSeconds,
            result.RefreshToken,
            result.RefreshExpiresInSeconds,
            result.SessionId,
            new UserResponse(result.User.Id, result.User.Email, result.User.Name));
}

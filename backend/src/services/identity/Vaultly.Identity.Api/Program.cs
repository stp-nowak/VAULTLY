using MediatR;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Serilog;

using Vaultly.Identity.Api;
using Vaultly.Identity.Api.Configuration;
using Vaultly.Identity.Application;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.UseCases.BuildAuthorizeUrl;
using Vaultly.Identity.Application.UseCases.ExchangeAuthCode;
using Vaultly.Identity.Application.UseCases.GetEnabledProviders;
using Vaultly.Identity.Application.UseCases.GetSessions;
using Vaultly.Identity.Application.UseCases.HandleOAuthCallback;
using Vaultly.Identity.Application.UseCases.Logout;
using Vaultly.Identity.Application.UseCases.RefreshSession;
using Vaultly.Identity.Application.UseCases.RevokeSession;
using Vaultly.Identity.Domain.Constants;
using Vaultly.Identity.Infrastructure;
using Vaultly.Identity.Infrastructure.Options;
using Vaultly.Identity.Infrastructure.Security;
using Vaultly.SharedKernel;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddOptionalDotEnvFile(Path.Combine(builder.Environment.ContentRootPath, ".env"));

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddSingleton(Log.Logger);

builder.Services.AddOpenApi();

builder.Services.Configure<IdentityOptions>(builder.Configuration.GetSection("Identity"));
builder.Services.Configure<GoogleOAuthOptions>(builder.Configuration.GetSection("Google"));

var identityOptions = builder.Configuration.GetSection("Identity").Get<IdentityOptions>()
                      ?? throw new InvalidOperationException("Identity configuration is missing.");

var resolvedJwtPrivateKey = JwtPrivateKeyResolver.Resolve(
    identityOptions,
    builder.Environment.ContentRootPath,
    builder.Environment.IsDevelopment());
Log.Information("Loaded JWT signing key from {JwtSigningKeySource}.", resolvedJwtPrivateKey.SourceDescription);

var rsaKeyProvider = new RsaKeyProvider(resolvedJwtPrivateKey.Pem);
builder.Services.AddSingleton(rsaKeyProvider);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = identityOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = identityOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaKeyProvider.SecurityKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

string? GetRedirectUri(HttpContext httpContext)
{
    var redirectUri = httpContext.Request.Query["redirectUri"].ToString();
    if (!string.IsNullOrWhiteSpace(redirectUri))
    {
        return redirectUri;
    }

    var legacyRedirectUri = httpContext.Request.Query["redirect_uri"].ToString();
    return string.IsNullOrWhiteSpace(legacyRedirectUri) ? null : legacyRedirectUri;
}

app.MapGet("/login", async (HttpContext httpContext, ISender sender) =>
{
    var redirectUri = GetRedirectUri(httpContext);
    var providers = await sender.Send(new GetEnabledProvidersQuery(), httpContext.RequestAborted);

    if (providers.Count == 1)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return Results.BadRequest(new { error = "Redirect URI is required." });
        }

        var url = QueryHelpers.AddQueryString(providers[0].AuthorizeUrl, new Dictionary<string, string?>
        {
            ["redirectUri"] = redirectUri
        });
        return Results.Redirect(url);
    }

    return Results.Ok(new
    {
        providers = providers.Select(provider => new
        {
            id = provider.Id,
            name = provider.Name,
            authorize_url = string.IsNullOrWhiteSpace(redirectUri)
                ? provider.AuthorizeUrl
                : QueryHelpers.AddQueryString(provider.AuthorizeUrl, "redirectUri", redirectUri)
        })
    });
});

app.MapGet("/google/authorize", async (HttpContext httpContext, ISender sender) =>
{
    var redirectUri = GetRedirectUri(httpContext);
    try
    {
        var redirectUrl = await sender.Send(
            new BuildAuthorizeUrlQuery(ProviderIds.Google.Value, redirectUri),
            httpContext.RequestAborted);
        return Results.Redirect(redirectUrl);
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/google/callback", async (HttpContext httpContext, ISender sender) =>
{
    var code = httpContext.Request.Query["code"].ToString();
    var state = httpContext.Request.Query["state"].ToString();
    var error = httpContext.Request.Query["error"].ToString();

    if (!string.IsNullOrWhiteSpace(error))
    {
        return Results.BadRequest(new { error });
    }

    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
    {
        return Results.BadRequest(new { error = "Missing code or state." });
    }

    var userAgent = httpContext.Request.Headers.UserAgent.ToString();
    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

    try
    {
        var callbackResult = await sender.Send(
            new HandleOAuthCallbackCommand(ProviderIds.Google.Value, code, state, userAgent, ipAddress),
            httpContext.RequestAborted);
        return Results.Redirect(callbackResult.RedirectUrl);
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/token", async (HttpContext httpContext, ISender sender, TokenRequest request) =>
{
    try
    {
        var result = await sender.Send(new ExchangeAuthCodeCommand(request.Code), httpContext.RequestAborted);
        AuthCookies.SetRefreshToken(httpContext.Response, result.RefreshToken, result.RefreshTokenExpiresAt, result.CookieDomain);
        return Results.Ok(new TokenResponse(result.AccessToken, result.ExpiresInSeconds, result.SessionId, result.User));
    }
    catch (DomainException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPost("/refresh", async (HttpContext httpContext, ISender sender) =>
{
    if (!AuthCookies.TryGetRefreshToken(httpContext.Request, out var refreshToken))
    {
        return Results.Unauthorized();
    }

    var result = await sender.Send(new RefreshSessionCommand(refreshToken), httpContext.RequestAborted);
    if (!result.Success)
    {
        AuthCookies.ClearRefreshToken(httpContext.Response, result.CookieDomain);
        return Results.Unauthorized();
    }

    AuthCookies.SetRefreshToken(httpContext.Response, result.RefreshToken!, result.RefreshTokenExpiresAt!.Value, result.CookieDomain);
    return Results.Ok(new TokenResponse(result.AccessToken!, result.ExpiresInSeconds, result.SessionId!.Value, result.User!));
});

app.MapPost("/logout", async (HttpContext httpContext, ISender sender, IOptions<IdentityOptions> options) =>
{
    string? refreshToken = null;
    if (AuthCookies.TryGetRefreshToken(httpContext.Request, out var token))
    {
        refreshToken = token;
    }

    await sender.Send(new LogoutCommand(refreshToken), httpContext.RequestAborted);

    AuthCookies.ClearRefreshToken(httpContext.Response, options.Value.RefreshTokenCookieDomain);
    return Results.NoContent();
});

app.MapGet("/sessions", async (HttpContext httpContext, ISender sender) =>
{
    var userId = httpContext.User.GetUserId();
    var currentSessionId = httpContext.User.GetSessionId();
    if (userId is null || currentSessionId is null)
    {
        return Results.Unauthorized();
    }

    var sessions = await sender.Send(
        new GetSessionsQuery(userId.Value, currentSessionId.Value),
        httpContext.RequestAborted);

    return Results.Ok(new { sessions });
}).RequireAuthorization();

app.MapDelete("/sessions/{id:guid}", async (HttpContext httpContext, ISender sender, IOptions<IdentityOptions> options, Guid id) =>
{
    var userId = httpContext.User.GetUserId();
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var revoked = await sender.Send(new RevokeSessionCommand(userId.Value, id), httpContext.RequestAborted);
    if (!revoked)
    {
        return Results.NotFound();
    }

    if (httpContext.User.GetSessionId() == id)
    {
        AuthCookies.ClearRefreshToken(httpContext.Response, options.Value.RefreshTokenCookieDomain);
    }

    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/.well-known/openid-configuration", (IOptions<IdentityOptions> options) =>
{
    var config = options.Value;
    return Results.Ok(new
    {
        issuer = config.Issuer,
        authorization_endpoint = $"{config.PublicBaseUrl.TrimEnd('/')}/login",
        token_endpoint = $"{config.PublicBaseUrl.TrimEnd('/')}/token",
        jwks_uri = $"{config.PublicBaseUrl.TrimEnd('/')}/.well-known/jwks.json",
        response_types_supported = new[] { "code" },
        subject_types_supported = new[] { "public" },
        id_token_signing_alg_values_supported = new[] { "RS256" },
        scopes_supported = new[] { "openid", "email", "profile" },
        token_endpoint_auth_methods_supported = new[] { "none" }
    });
});

app.MapGet("/.well-known/jwks.json", (IJwtKeyProvider keyProvider) =>
{
    var jwk = keyProvider.GetJwk();
    return Results.Ok(new { keys = new[] { jwk } });
});

app.Run();

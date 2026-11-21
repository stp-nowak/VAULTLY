using MediatR;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Serilog;

using Vaultly.Identity.Api;
using Vaultly.Identity.Api.Configuration;
using Vaultly.Identity.Application;
using Vaultly.Identity.Application.Commands.RevokeSession;
using Vaultly.Identity.Application.Interfaces.Services;
using Vaultly.Identity.Application.Options;
using Vaultly.Identity.Application.Queries.GetSessions;
using Vaultly.Identity.Infrastructure;
using Vaultly.Identity.Infrastructure.Options;
using Vaultly.Identity.Infrastructure.Security;

// Vaultly Identity: OAuth 2.0 / OIDC authorization server (authorization code + PKCE, SSO session) that also hosts
// its own login UI (auth/web). Client applications only redirect to /authorize and call /token server-to-server.
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
builder.Services.Configure<LoginUiOptions>(builder.Configuration.GetSection(LoginUiOptions.SectionName));

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

// No CORS: browsers only navigate to this service (or reach it same-origin via the login UI); clients call it
// server-to-server.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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
else
{
    app.UseHttpsRedirection();
}

app.UseLoginUiStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

var endpointErrorHandler = new EndpointErrorHandler(Log.ForContext<EndpointErrorHandler>());

app.MapOAuthEndpoints();

app.MapGet("/sessions", async (HttpContext httpContext, ISender sender) =>
    await endpointErrorHandler.ExecuteAsync(httpContext, "GET /sessions", async () =>
    {
        var userId = httpContext.User.GetUserId();
        var currentSessionId = httpContext.User.GetSessionId();
        if (userId is null || currentSessionId is null)
        {
            return endpointErrorHandler.Unauthorized(httpContext, "GET /sessions", "Required session claims are missing.");
        }

        var sessions = await sender.Send(
            new GetSessionsQuery(userId.Value, currentSessionId.Value),
            httpContext.RequestAborted);

        return Results.Ok(new { sessions });
    })).RequireAuthorization();

app.MapDelete("/sessions/{id:guid}", async (HttpContext httpContext, ISender sender, Guid id) =>
    await endpointErrorHandler.ExecuteAsync(httpContext, "DELETE /sessions/{id}", async () =>
    {
        var userId = httpContext.User.GetUserId();
        if (userId is null)
        {
            return endpointErrorHandler.Unauthorized(httpContext, "DELETE /sessions/{id}", "User claim is missing.");
        }

        var revoked = await sender.Send(new RevokeSessionCommand(userId.Value, id), httpContext.RequestAborted);
        return revoked
            ? Results.NoContent()
            : endpointErrorHandler.NotFound(httpContext, "DELETE /sessions/{id}", "Session was not found for the current user.");
    })).RequireAuthorization();

app.MapGet("/.well-known/openid-configuration", (IOptions<IdentityOptions> options) =>
{
    var config = options.Value;
    var baseUrl = config.PublicBaseUrl.TrimEnd('/');
    return Results.Ok(new
    {
        issuer = config.Issuer,
        authorization_endpoint = $"{baseUrl}/authorize",
        token_endpoint = $"{baseUrl}/token",
        revocation_endpoint = $"{baseUrl}/revoke",
        end_session_endpoint = $"{baseUrl}/logout",
        jwks_uri = $"{baseUrl}/.well-known/jwks.json",
        response_types_supported = new[] { "code" },
        grant_types_supported = new[] { "authorization_code", "refresh_token" },
        code_challenge_methods_supported = new[] { "S256" },
        subject_types_supported = new[] { "public" },
        id_token_signing_alg_values_supported = new[] { "RS256" },
        scopes_supported = new[] { "openid", "email", "profile" },
        token_endpoint_auth_methods_supported = new[] { "client_secret_basic", "client_secret_post" }
    });
});

app.MapGet("/.well-known/jwks.json", (IJwtKeyProvider keyProvider) =>
{
    var jwk = keyProvider.GetJwk();
    return Results.Ok(new { keys = new[] { jwk } });
});

app.MapLoginUi();

app.Run();

/// <summary>
/// Entry point; public partial so integration tests can host the API with <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;

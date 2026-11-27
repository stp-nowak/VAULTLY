using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

using Vaultly.Identity.PublicTypes;

namespace Vaultly.Core.Authentication;

/// <summary>
/// Makes a service accept only tokens the gateway issued for it; every endpoint requires one unless it opts out
/// with <c>AllowAnonymous</c>. The gateway copies the user's claims as they are, so they are read with
/// <see cref="IdentityClaims"/>.
/// </summary>
public static class InternalTokenAuthentication
{
    /// <summary>Named HTTP client that downloads the gateway's signing keys.</summary>
    public const string KeysHttpClient = "InternalTokenKeys";

    public static IServiceCollection AddInternalTokenAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<InternalTokenOptions>()
            .Bind(configuration.GetSection(InternalTokenOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient(KeysHttpClient);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<InternalTokenOptions>, IHttpClientFactory, IHostEnvironment>(Configure);

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    /// <summary>
    /// The signed-in user's id; only valid on endpoints that require authentication.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue(IdentityClaims.UserId), out var userId)
            ? userId
            : throw new InvalidOperationException("The principal carries no user id.");

    private static void Configure(
        JwtBearerOptions options,
        IOptions<InternalTokenOptions> internalTokens,
        IHttpClientFactory httpClients,
        IHostEnvironment environment)
    {
        var tokens = internalTokens.Value;

        options.MapInboundClaims = false;
        options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            tokens.JwksUrl,
            new JwksConfigurationRetriever(),
            new HttpDocumentRetriever(httpClients.CreateClient(KeysHttpClient)) { RequireHttps = !environment.IsDevelopment() });
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = tokens.Issuer,
            ValidAudience = tokens.Audience,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            NameClaimType = IdentityClaims.UserId,
            RoleClaimType = IdentityClaims.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }
}

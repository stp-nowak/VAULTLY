using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

using Vaultly.Gateway.Http;
using Vaultly.Identity.PublicTypes;

namespace Vaultly.Gateway.UserTokens;

/// <summary>
/// Authorization policy names that routes may set as <c>AuthorizationPolicy</c> in the <c>ReverseProxy</c> section.
/// A route without one requires any signed-in user; YARP's built-in <c>anonymous</c> lets everyone through.
/// </summary>
public static class GatewayPolicies
{
    public const string Admin = "admin";
}

/// <summary>
/// Validates the Identity service's access tokens on every request before anything is forwarded.
/// </summary>
public static class UserTokenAuthentication
{
    /// <summary>Named HTTP client that downloads Identity's signing keys.</summary>
    public const string KeysHttpClient = "UserTokenKeys";

    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddUserTokens(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<UserTokenOptions>()
            .Bind(configuration.GetSection(UserTokenOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient(KeysHttpClient);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<UserTokenOptions>, IHttpClientFactory, IHostEnvironment>(Configure);

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(GatewayPolicies.Admin, policy => policy.RequireClaim(IdentityClaims.Role, IdentityClaims.ValueOf(UserRole.Admin)));

        services.AddHealthChecks().AddCheck<UserTokenKeysHealthCheck>("user-token-keys", tags: [HealthTags.Ready]);

        return services;
    }

    private static void Configure(
        JwtBearerOptions options,
        IOptions<UserTokenOptions> userTokens,
        IHttpClientFactory httpClients,
        IHostEnvironment environment)
    {
        var tokens = userTokens.Value;

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
            ClockSkew = ClockSkew
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = WriteUnauthorizedAsync,
            OnForbidden = context => GatewayProblems.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden, "Not allowed.")
        };
    }

    private static Task WriteUnauthorizedAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        context.Response.Headers[HeaderNames.WWWAuthenticate] = context.AuthenticateFailure is null
            ? JwtBearerDefaults.AuthenticationScheme
            : $"{JwtBearerDefaults.AuthenticationScheme} error=\"invalid_token\"";

        return GatewayProblems.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "A valid access token is required.");
    }
}

/// <summary>
/// Ready only once Identity's signing keys are loaded; until then every request would be rejected.
/// </summary>
internal sealed class UserTokenKeysHealthCheck(IOptionsMonitor<JwtBearerOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await options.Get(JwtBearerDefaults.AuthenticationScheme).ConfigurationManager!
                .GetConfigurationAsync(cancellationToken);

            return configuration.SigningKeys.Count > 0
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("The Identity service published no signing keys.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("The Identity service's signing keys cannot be loaded.", exception);
        }
    }
}

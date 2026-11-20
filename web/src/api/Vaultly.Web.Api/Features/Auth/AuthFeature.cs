using Microsoft.Extensions.Options;

using Vaultly.Web.Api.Features.Auth.IdentityService;
using Vaultly.Web.Api.Features.Auth.Sessions;

namespace Vaultly.Web.Api.Features.Auth;

/// <summary>
/// Composition of the Auth feature: login via the Identity service, server-side session with tokens in Redis,
/// session cookie for the browser. Other features may depend only on <see cref="IAccessTokenProvider"/>.
/// </summary>
public static class AuthFeature
{
    /// <summary>
    /// Registers options, the Identity service client and session services.
    /// </summary>
    public static IServiceCollection AddAuthFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AuthOptions>>().Value;
                client.BaseAddress = new Uri(options.IdentityBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(15);
            })
            // Cookies are passed explicitly per call (refresh token); a shared cookie jar would leak between users.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { UseCookies = false, AllowAutoRedirect = false });

        services.AddSingleton<IWebSessionStore, DistributedWebSessionStore>();
        services.AddSingleton<SessionRefreshLock>();
        services.AddSingleton<SessionCookie>();
        services.AddSingleton<LoginStateCookie>();

        services.AddScoped<UserSessionService>();
        services.AddScoped<IUserSessionService>(serviceProvider => serviceProvider.GetRequiredService<UserSessionService>());
        services.AddScoped<IAccessTokenProvider>(serviceProvider => serviceProvider.GetRequiredService<UserSessionService>());

        return services;
    }

    /// <summary>
    /// Maps the Auth feature endpoints under <see cref="AuthEndpoints.RoutePrefix"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapAuthFeature(this IEndpointRouteBuilder endpoints) => endpoints.MapAuthEndpoints();
}

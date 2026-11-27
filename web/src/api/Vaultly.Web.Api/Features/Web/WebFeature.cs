using Vaultly.Web.Api.Features.Web.ApiProxy;

namespace Vaultly.Web.Api.Features.Web;

/// <summary>
/// Composition of the Web feature: everything the SPA needs besides authentication — forwarding API calls to the
/// gateway with the session's token. The SPA itself is hosted by its own web server, never by this API.
/// Depends on the Auth feature only through <see cref="Auth.Sessions.IAccessTokenProvider"/>.
/// </summary>
public static class WebFeature
{
    /// <summary>
    /// Registers options and the YARP forwarder.
    /// </summary>
    public static IServiceCollection AddWebFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WebOptions>()
            .Bind(configuration.GetSection(WebOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpForwarder();
        services.AddSingleton(_ => GatewayHttpClient.Create());

        return services;
    }

    /// <summary>
    /// Maps the API proxy.
    /// </summary>
    public static IEndpointRouteBuilder MapWebFeature(this IEndpointRouteBuilder endpoints) => endpoints.MapApiProxy();
}

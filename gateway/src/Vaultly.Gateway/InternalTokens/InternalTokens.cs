using System.Net.Http.Headers;

using Microsoft.Extensions.Options;

using Yarp.ReverseProxy.Transforms;

namespace Vaultly.Gateway.InternalTokens;

/// <summary>
/// Replaces the user's access token with an internal token on every forwarded request, so services never see a
/// token that is valid anywhere else.
/// </summary>
public static class InternalTokens
{
    public static IServiceCollection AddInternalTokens(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<InternalTokenOptions>()
            .Bind(configuration.GetSection(InternalTokenOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(provider => InternalTokenSigningKey.Load(
            provider.GetRequiredService<IOptions<InternalTokenOptions>>(),
            provider.GetRequiredService<IHostEnvironment>()));
        services.AddSingleton<InternalTokenIssuer>();

        return services;
    }

    /// <summary>
    /// The route's cluster id is the audience: a token issued for one service is rejected by every other one.
    /// </summary>
    public static IReverseProxyBuilder AddInternalTokenTransform(this IReverseProxyBuilder proxy)
        => proxy.AddTransforms(context =>
        {
            if (context.Route.ClusterId is not { } audience)
            {
                return;
            }

            context.AddRequestTransform(transform =>
            {
                var headers = transform.ProxyRequest.Headers;
                headers.Authorization = null;

                var user = transform.HttpContext.User;
                if (user.Identity?.IsAuthenticated == true)
                {
                    var token = transform.HttpContext.RequestServices.GetRequiredService<InternalTokenIssuer>().Issue(user, audience);
                    headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                return ValueTask.CompletedTask;
            });
        });

    public static IEndpointRouteBuilder MapInternalTokenKeys(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/.well-known/jwks.json", (InternalTokenSigningKey key) => Results.Json(new
            {
                keys = new[]
                {
                    new { kty = key.PublicJwk.Kty, use = key.PublicJwk.Use, alg = key.PublicJwk.Alg, kid = key.PublicJwk.Kid, n = key.PublicJwk.N, e = key.PublicJwk.E }
                }
            }))
            .AllowAnonymous();
        return endpoints;
    }
}

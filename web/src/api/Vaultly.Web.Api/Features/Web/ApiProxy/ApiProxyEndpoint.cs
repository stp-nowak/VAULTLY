using System.Diagnostics;
using System.Net;

using Microsoft.Extensions.Options;

using Vaultly.Web.Api.Features.Auth.Sessions;

using Yarp.ReverseProxy.Forwarder;

namespace Vaultly.Web.Api.Features.Web.ApiProxy;

/// <summary>
/// HTTP client used to forward requests to the backend. Wrapped in its own type so tests can substitute it.
/// </summary>
public sealed class BackendHttpClient(HttpMessageInvoker invoker)
{
    /// <summary>Underlying invoker shared by all forwarded requests.</summary>
    public HttpMessageInvoker Invoker { get; } = invoker;

    /// <summary>
    /// Creates the invoker recommended for YARP direct forwarding: no cookies, redirects, decompression or proxy,
    /// so responses stream back to the browser unchanged.
    /// </summary>
    public static BackendHttpClient Create()
        => new(new HttpMessageInvoker(new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,
            EnableMultipleHttp2Connections = true,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15),
        }));
}

/// <summary>
/// Catch-all <c>/api/{**path}</c> endpoint forwarding SPA calls to the backend with the session's bearer token.
/// More specific routes (e.g. <c>/api/auth/*</c>) win over this catch-all.
/// </summary>
public static class ApiProxyEndpoint
{
    /// <summary>
    /// Maps the proxy endpoint for every HTTP method.
    /// </summary>
    public static IEndpointRouteBuilder MapApiProxy(this IEndpointRouteBuilder endpoints)
    {
        endpoints.Map("/api/{**path}", ForwardAsync).WithTags("Web");
        return endpoints;
    }

    /// <summary>
    /// Returns 401 without a session, otherwise forwards the request and streams the backend response back.
    /// </summary>
    internal static async Task ForwardAsync(HttpContext context)
    {
        var services = context.RequestServices;
        var accessToken = await services.GetRequiredService<IAccessTokenProvider>()
            .GetAccessTokenAsync(context, context.RequestAborted);

        if (accessToken is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var options = services.GetRequiredService<IOptions<WebOptions>>().Value;
        var error = await services.GetRequiredService<IHttpForwarder>().SendAsync(
            context,
            options.BackendApiBaseUrl,
            services.GetRequiredService<BackendHttpClient>().Invoker,
            ForwarderRequestConfig.Empty,
            new BackendApiTransformer(accessToken));

        if (error != ForwarderError.None)
        {
            // YARP already wrote 502/504; log the cause for diagnostics (no tokens in the exception).
            services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(ApiProxyEndpoint))
                .LogWarning(
                    context.GetForwarderErrorFeature()?.Exception,
                    "Forwarding {Method} {Path} to backend failed with {ForwarderError}",
                    context.Request.Method,
                    context.Request.Path,
                    error);
        }
    }
}

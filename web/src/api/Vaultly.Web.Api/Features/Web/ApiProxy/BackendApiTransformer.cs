using System.Net.Http.Headers;

using Microsoft.Net.Http.Headers;

using Vaultly.Web.Api.Common.Csrf;

using Yarp.ReverseProxy.Forwarder;

namespace Vaultly.Web.Api.Features.Web.ApiProxy;

/// <summary>
/// Turns a browser request into a backend request: strips the <c>/api</c> prefix, drops browser-only credentials
/// (cookies, CSRF header) and authenticates with the session's access token.
/// </summary>
public sealed class BackendApiTransformer(string accessToken) : HttpTransformer
{
    /// <summary>Path prefix handled by the proxy.</summary>
    public static readonly PathString ApiPrefix = new("/api");

    private readonly string _accessToken = accessToken;

    /// <inheritdoc />
    public override async ValueTask TransformRequestAsync(
        HttpContext httpContext,
        HttpRequestMessage proxyRequest,
        string destinationPrefix,
        CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

        var path = httpContext.Request.Path.StartsWithSegments(ApiPrefix, out var remaining) ? remaining : httpContext.Request.Path;
        proxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress(destinationPrefix, path, httpContext.Request.QueryString);

        // The session cookie is a credential for this API only; the backend must never see it.
        proxyRequest.Headers.Remove(HeaderNames.Cookie);
        proxyRequest.Headers.Remove(CsrfHeaderMiddleware.HeaderName);
        proxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
    }
}

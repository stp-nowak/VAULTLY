namespace Vaultly.Identity.Api;

/// <summary>
/// Requires <c>X-CSRF: 1</c> on JSON calls from the login UI and the admin console, and marks responses
/// <c>no-store</c>. A cross-site page cannot add a custom header without a CORS preflight (and there is no CORS), so
/// calls riding on the browser's SSO cookie, or signing a browser in, must come from this service's own UI.
/// </summary>
public static class CsrfHeaderFilter
{
    public const string HeaderName = "X-CSRF";

    public static ValueTask<object?> Require(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        httpContext.Response.Headers.CacheControl = "no-store";

        return httpContext.Request.Headers[HeaderName] == "1"
            ? next(context)
            : ValueTask.FromResult<object?>(Results.BadRequest(new { error = "csrf_header_missing" }));
    }
}

namespace Vaultly.Web.Api.Common.Csrf;

/// <summary>
/// CSRF protection for cookie-authenticated endpoints. Every request to <c>/api</c> must carry
/// <c>X-CSRF: 1</c>. Browsers cannot attach custom headers to cross-site requests without a CORS preflight and the
/// web API never allows cross-origin calls, so the header proves the request was issued by our own SPA.
/// Browser navigations (login, OAuth callback) opt out with <see cref="CsrfEndpointConventionExtensions.AllowWithoutCsrfHeader{TBuilder}"/>.
/// </summary>
public sealed class CsrfHeaderMiddleware(RequestDelegate next, ILogger<CsrfHeaderMiddleware> logger)
{
    /// <summary>Name of the anti-forgery header sent by the SPA.</summary>
    public const string HeaderName = "X-CSRF";

    /// <summary>Expected value of the anti-forgery header.</summary>
    public const string HeaderValue = "1";

    private static readonly PathString[] ProtectedPrefixes = [new("/api")];

    private readonly RequestDelegate _next = next;
    private readonly ILogger<CsrfHeaderMiddleware> _logger = logger;

    /// <summary>
    /// Rejects protected requests that do not carry the anti-forgery header. Must run after routing so endpoint
    /// metadata is available.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresHeader(context) && !HasValidHeader(context.Request))
        {
            _logger.LogWarning(
                "Rejected {Method} {Path}: missing {Header} header",
                context.Request.Method,
                context.Request.Path,
                HeaderName);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = $"Missing {HeaderName} header." });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Determines whether the current request targets a protected path that has not opted out.
    /// </summary>
    private static bool RequiresHeader(HttpContext context)
    {
        if (!ProtectedPrefixes.Any(prefix => context.Request.Path.StartsWithSegments(prefix)))
        {
            return false;
        }

        return context.GetEndpoint()?.Metadata.GetMetadata<AllowWithoutCsrfHeaderMetadata>() is null;
    }

    /// <summary>
    /// Checks that the anti-forgery header is present with the expected value.
    /// </summary>
    private static bool HasValidHeader(HttpRequest request)
        => string.Equals(request.Headers[HeaderName], HeaderValue, StringComparison.Ordinal);
}

/// <summary>
/// Endpoint metadata marking a browser-navigation endpoint that cannot send custom headers.
/// </summary>
public sealed class AllowWithoutCsrfHeaderMetadata;

/// <summary>
/// Endpoint convention helpers for the CSRF header check.
/// </summary>
public static class CsrfEndpointConventionExtensions
{
    /// <summary>
    /// Opts an endpoint out of the CSRF header check. Use only for top-level GET navigations that never change
    /// state on behalf of an existing session (login start, OAuth callback with its own state validation).
    /// </summary>
    public static TBuilder AllowWithoutCsrfHeader<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.WithMetadata(new AllowWithoutCsrfHeaderMetadata());
}

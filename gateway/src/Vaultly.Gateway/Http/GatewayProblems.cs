namespace Vaultly.Gateway.Http;

/// <summary>
/// Responses the gateway produces itself (not the ones streamed back from a service) as RFC 9457 problem details.
/// </summary>
internal static class GatewayProblems
{
    public static Task WriteAsync(HttpContext context, int statusCode, string title)
        => Results.Problem(statusCode: statusCode, title: title).ExecuteAsync(context);

    /// <summary>
    /// YARP answers a failed forward with a bare 502/503/504; this gives it a problem body.
    /// </summary>
    public static async Task DescribeForwarderErrors(HttpContext context, Func<Task> next)
    {
        await next();

        if (context.GetForwarderErrorFeature() is not null && !context.Response.HasStarted)
        {
            await WriteAsync(context, context.Response.StatusCode, "The service did not respond.");
        }
    }
}

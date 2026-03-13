using Vaultly.SharedKernel;

namespace Vaultly.Identity.Api;

public sealed class EndpointErrorHandler(Serilog.ILogger logger)
{
    /// <summary>
    /// Executes endpoint logic and converts known failures into logged HTTP responses.
    /// </summary>
    public async Task<IResult> ExecuteAsync(HttpContext httpContext, string endpointName, Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (DomainException ex)
        {
            return BadRequest(httpContext, endpointName, ex.Message, exception: ex);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(httpContext, endpointName, ex.Message, exception: ex);
        }
        catch (Exception ex)
        {
            logger.Error(
                ex,
                "Endpoint {EndpointName} failed unexpectedly at {RequestMethod} {RequestPath}. UserId: {UserId}; SessionId: {SessionId}",
                endpointName,
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? string.Empty,
                httpContext.User.GetUserId(),
                httpContext.User.GetSessionId());

            return Results.Problem(
                title: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Logs a bad request reason and returns a 400 response with the supplied payload.
    /// </summary>
    public IResult BadRequest(HttpContext httpContext, string endpointName, string reason, object? responseBody = null, Exception? exception = null)
    {
        LogWarning(httpContext, endpointName, reason, exception, "bad request");
        return Results.BadRequest(responseBody ?? new { error = reason });
    }

    /// <summary>
    /// Logs an unauthorized outcome and returns a 401 response without exposing sensitive details.
    /// </summary>
    public IResult Unauthorized(HttpContext httpContext, string endpointName, string reason)
    {
        LogWarning(httpContext, endpointName, reason, exception: null, outcome: "unauthorized");
        return Results.Unauthorized();
    }

    /// <summary>
    /// Logs a missing resource outcome and returns a 404 response.
    /// </summary>
    public IResult NotFound(HttpContext httpContext, string endpointName, string reason)
    {
        LogWarning(httpContext, endpointName, reason, exception: null, outcome: "not found");
        return Results.NotFound();
    }

    /// <summary>
    /// Writes a warning log entry for an endpoint failure using safe request context only.
    /// </summary>
    private void LogWarning(HttpContext httpContext, string endpointName, string reason, Exception? exception, string outcome)
    {
        const string MessageTemplate =
            "Endpoint {EndpointName} returned {Outcome} at {RequestMethod} {RequestPath}. UserId: {UserId}; SessionId: {SessionId}; Reason: {Reason}";

        if (exception is null)
        {
            logger.Warning(
                MessageTemplate,
                endpointName,
                outcome,
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? string.Empty,
                httpContext.User.GetUserId(),
                httpContext.User.GetSessionId(),
                reason);
            return;
        }

        logger.Warning(
            exception,
            MessageTemplate,
            endpointName,
            outcome,
            httpContext.Request.Method,
            httpContext.Request.Path.Value ?? string.Empty,
            httpContext.User.GetUserId(),
            httpContext.User.GetSessionId(),
            reason);
    }
}



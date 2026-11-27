using MediatR;

using Vaultly.Identity.Api.Http;
using Vaultly.Identity.ApiContracts;
using Vaultly.Identity.Application.Features.Sessions.GetSessions;
using Vaultly.Identity.Application.Features.Sessions.RevokeSession;

namespace Vaultly.Identity.Api.Endpoints;

/// <summary>
/// The signed-in user's sessions, authenticated with a bearer access token.
/// </summary>
public static class SessionEndpoints
{
    /// <summary>
    /// Maps <c>GET /sessions</c> and <c>DELETE /sessions/{id}</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sessions", GetSessionsAsync).RequireAuthorization();
        app.MapDelete("/sessions/{id:guid}", RevokeSessionAsync).RequireAuthorization();
        return app;
    }

    internal static Task<IResult> GetSessionsAsync(HttpContext httpContext, ISender sender, EndpointErrorHandler errors)
        => errors.ExecuteAsync(httpContext, "GET /sessions", async () =>
        {
            var userId = httpContext.User.GetUserId();
            var currentSessionId = httpContext.User.GetSessionId();
            if (userId is null || currentSessionId is null)
            {
                return errors.Unauthorized(httpContext, "GET /sessions", "Required session claims are missing.");
            }

            var sessions = await sender.Send(new GetSessionsQuery(userId.Value, currentSessionId.Value), httpContext.RequestAborted);
            return Results.Ok(new SessionsResponse([.. sessions.Select(session => new SessionResponse(
                session.Id,
                session.CreatedAt,
                session.LastUsedAt,
                session.UserAgent,
                session.IpAddress,
                session.IsRevoked,
                session.IsCurrent))]));
        });

    internal static Task<IResult> RevokeSessionAsync(Guid id, HttpContext httpContext, ISender sender, EndpointErrorHandler errors)
        => errors.ExecuteAsync(httpContext, "DELETE /sessions/{id}", async () =>
        {
            var userId = httpContext.User.GetUserId();
            if (userId is null)
            {
                return errors.Unauthorized(httpContext, "DELETE /sessions/{id}", "User claim is missing.");
            }

            var revoked = await sender.Send(new RevokeSessionCommand(userId.Value, id), httpContext.RequestAborted);
            return revoked
                ? Results.NoContent()
                : errors.NotFound(httpContext, "DELETE /sessions/{id}", "Session was not found for the current user.");
        });
}

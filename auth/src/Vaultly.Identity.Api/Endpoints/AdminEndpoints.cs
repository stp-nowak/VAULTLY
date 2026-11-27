using MediatR;

using Vaultly.Identity.Api.Contracts;
using Vaultly.Identity.Api.Http;
using Vaultly.Identity.Application.Commands.AssignUserRole;
using Vaultly.Identity.Application.Commands.SetSignInMethodEnabled;
using Vaultly.Identity.Application.Commands.StartConsoleSignIn;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Queries.GetConsoleUser;
using Vaultly.Identity.Application.Queries.GetSignInSettings;
using Vaultly.Identity.Application.Queries.GetUsers;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.PublicTypes;

namespace Vaultly.Identity.Api.Endpoints;

/// <summary>
/// API of the admin console (login UI <c>/admin</c>). The console is authenticated by the SSO cookie, and the role is
/// read from the database on every call, so revoking a session or a role takes effect at once.
/// </summary>
public static class AdminEndpoints
{
    public const int DefaultPageSize = 20;

    private const string AdminItemKey = "vaultly.console-admin";

    /// <summary>
    /// Maps <c>GET /api/admin/sign-in</c> (a navigation) and the <c>/api/admin</c> JSON API for admins.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/sign-in", SignInAsync);

        var api = app.MapGroup("/api/admin")
            .AddEndpointFilter(CsrfHeaderFilter.Require)
            .AddEndpointFilter(RequireAdminAsync);

        api.MapGet("/me", GetCurrentAdmin);
        api.MapGet("/users", GetUsersAsync);
        api.MapPut("/users/{id:guid}/role", AssignRoleAsync);
        api.MapGet("/sign-in-settings", GetSignInSettingsAsync);
        api.MapPut("/sign-in-settings/methods/{method:regex(^[a-z0-9-]+$)}", SetSignInMethodEnabledAsync);
        return app;
    }

    internal static async Task<IResult> SignInAsync(HttpContext context, ISender sender)
    {
        var result = await sender.Send(new StartConsoleSignInCommand(SsoCookie.Read(context.Request)), context.RequestAborted);
        return Results.Redirect(result.RedirectUrl);
    }

    internal static async ValueTask<object?> RequireAdminAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var sender = httpContext.RequestServices.GetRequiredService<ISender>();
        var user = await sender.Send(new GetConsoleUserQuery(SsoCookie.Read(httpContext.Request)), httpContext.RequestAborted);

        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (user.Role != UserRole.Admin)
        {
            Logger(httpContext).LogWarning("User {UserId} without the admin role tried to use the admin console", user.Id);
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        httpContext.Items[AdminItemKey] = user;
        return await next(context);
    }

    internal static IResult GetCurrentAdmin(HttpContext context) => Results.Ok(CurrentAdmin(context));

    internal static async Task<IResult> GetUsersAsync(string? search, int? page, int? pageSize, ISender sender, CancellationToken cancellationToken)
        => Results.Ok(await sender.Send(new GetUsersQuery(search, page ?? 1, pageSize ?? DefaultPageSize), cancellationToken));

    internal static async Task<IResult> AssignRoleAsync(Guid id, AssignRoleRequest body, HttpContext context, ISender sender)
    {
        try
        {
            var assigned = await sender.Send(new AssignUserRoleCommand(CurrentAdmin(context).Id, id, body.Role), context.RequestAborted);
            return assigned ? Results.NoContent() : Results.NotFound();
        }
        catch (LastAdminException)
        {
            return Results.Conflict(new ApiErrorResponse(ApiError.LastAdmin));
        }
    }

    internal static async Task<IResult> GetSignInSettingsAsync(ISender sender, CancellationToken cancellationToken)
        => Results.Ok(new { methods = await sender.Send(new GetSignInSettingsQuery(), cancellationToken) });

    internal static async Task<IResult> SetSignInMethodEnabledAsync(string method, SetSignInMethodRequest body, HttpContext context, ISender sender)
    {
        try
        {
            var changed = await sender.Send(
                new SetSignInMethodEnabledCommand(CurrentAdmin(context).Id, method, body.Enabled),
                context.RequestAborted);
            return changed ? Results.NoContent() : Results.NotFound();
        }
        catch (LastSignInMethodException)
        {
            return Results.Conflict(new ApiErrorResponse(ApiError.LastSignInMethod));
        }
    }

    private static ConsoleUserDto CurrentAdmin(HttpContext context) => (ConsoleUserDto)context.Items[AdminItemKey]!;

    private static ILogger Logger(HttpContext context)
        => context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AdminEndpoints));

    /// <summary>Body of <c>PUT /api/admin/users/{id}/role</c>.</summary>
    public sealed record AssignRoleRequest(UserRole Role);

    /// <summary>Body of <c>PUT /api/admin/sign-in-settings/methods/{method}</c>.</summary>
    public sealed record SetSignInMethodRequest(bool Enabled);
}

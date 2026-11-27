using System.Security.Claims;

using MediatR;

using Microsoft.AspNetCore.Http.HttpResults;

using Vaultly.Accounts.Application.Features.NetWorth.GetNetWorth;
using Vaultly.Core.Authentication;

namespace Vaultly.Accounts.Api.Endpoints;

public static class NetWorthEndpoints
{
    public static IEndpointRouteBuilder MapNetWorthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/accounts/net-worth", GetNetWorthAsync);
        return app;
    }

    internal static async Task<Ok<IReadOnlyList<CurrencyNetWorthDto>>> GetNetWorthAsync(
        ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
        => TypedResults.Ok(await sender.Send(new GetNetWorthQuery(user.GetUserId()), cancellationToken));
}

using System.Security.Claims;

using MediatR;

using Microsoft.AspNetCore.Http.HttpResults;

using Vaultly.Accounts.Api.Contracts;
using Vaultly.Accounts.Application.Features.Accounts;
using Vaultly.Accounts.Application.Features.Accounts.CloseAccount;
using Vaultly.Accounts.Application.Features.Accounts.GetAccount;
using Vaultly.Accounts.Application.Features.Accounts.GetBalanceHistory;
using Vaultly.Accounts.Application.Features.Accounts.GetAccounts;
using Vaultly.Accounts.Application.Features.Accounts.OpenAccount;
using Vaultly.Accounts.Application.Features.Accounts.RenameAccount;
using Vaultly.Core.Authentication;

namespace Vaultly.Accounts.Api.Endpoints;

/// <summary>The signed-in user's accounts; another user's account answers 404.</summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var accounts = app.MapGroup("/accounts");
        accounts.MapGet("", GetAccountsAsync);
        accounts.MapPost("", OpenAccountAsync);
        accounts.MapGet("/{id:guid}", GetAccountAsync);
        accounts.MapPut("/{id:guid}/name", RenameAccountAsync);
        accounts.MapPost("/{id:guid}/close", CloseAccountAsync);
        accounts.MapGet("/{id:guid}/balances", GetBalanceHistoryAsync);
        return app;
    }

    internal static async Task<Ok<IReadOnlyList<AccountDto>>> GetAccountsAsync(
        ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken, bool includeClosed = false)
        => TypedResults.Ok(await sender.Send(new GetAccountsQuery(user.GetUserId(), includeClosed), cancellationToken));

    internal static async Task<Results<Created<AccountDto>, ValidationProblem>> OpenAccountAsync(
        OpenAccountRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        if (request.Type is not { } type)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["type"] = ["Account type is required."] });
        }

        var ownerId = user.GetUserId();
        var accountId = await sender.Send(
            new OpenAccountCommand(ownerId, request.Name, type, request.Currency, request.OpeningBalance),
            cancellationToken);
        var account = await sender.Send(new GetAccountQuery(ownerId, accountId), cancellationToken);
        return TypedResults.Created($"/accounts/{accountId}", account);
    }

    internal static async Task<Ok<AccountDto>> GetAccountAsync(
        Guid id, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
        => TypedResults.Ok(await sender.Send(new GetAccountQuery(user.GetUserId(), id), cancellationToken));

    internal static async Task<NoContent> RenameAccountAsync(
        Guid id, RenameAccountRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new RenameAccountCommand(user.GetUserId(), id, request.Name), cancellationToken);
        return TypedResults.NoContent();
    }

    internal static async Task<Ok<IReadOnlyList<DailyBalanceDto>>> GetBalanceHistoryAsync(
        Guid id, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken, DateOnly? from = null, DateOnly? to = null)
        => TypedResults.Ok(await sender.Send(new GetBalanceHistoryQuery(user.GetUserId(), id, from, to), cancellationToken));

    internal static async Task<NoContent> CloseAccountAsync(
        Guid id, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new CloseAccountCommand(user.GetUserId(), id), cancellationToken);
        return TypedResults.NoContent();
    }
}

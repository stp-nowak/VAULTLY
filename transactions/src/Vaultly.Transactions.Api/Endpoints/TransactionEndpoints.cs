using System.Security.Claims;

using MediatR;

using Microsoft.AspNetCore.Http.HttpResults;

using Vaultly.Core.Authentication;
using Vaultly.Transactions.Api.Contracts;
using Vaultly.Transactions.Application.Features.Transactions;
using Vaultly.Transactions.Application.Features.Transactions.ChangeTransaction;
using Vaultly.Transactions.Application.Features.Transactions.DeleteTransaction;
using Vaultly.Transactions.Application.Features.Transactions.GetTransaction;
using Vaultly.Transactions.Application.Features.Transactions.GetTransactions;
using Vaultly.Transactions.Application.Features.Transactions.RecordTransaction;

namespace Vaultly.Transactions.Api.Endpoints;

/// <summary>
/// The signed-in user's transactions; another user's transaction answers 404. A change that Accounts has to apply to
/// the balance answers 202: the transaction is pending until it does.
/// </summary>
public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var transactions = app.MapGroup("/transactions");
        transactions.MapGet("", GetTransactionsAsync);
        transactions.MapPost("", RecordTransactionAsync);
        transactions.MapGet("/{id:guid}", GetTransactionAsync);
        transactions.MapPut("/{id:guid}", ChangeTransactionAsync);
        transactions.MapDelete("/{id:guid}", DeleteTransactionAsync);
        return app;
    }

    internal static async Task<Ok<IReadOnlyList<TransactionDto>>> GetTransactionsAsync(
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken,
        Guid? accountId = null,
        DateOnly? from = null,
        DateOnly? to = null)
        => TypedResults.Ok(await sender.Send(new GetTransactionsQuery(user.GetUserId(), accountId, from, to), cancellationToken));

    internal static async Task<Created<TransactionDto>> RecordTransactionAsync(
        RecordTransactionRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        var ownerId = user.GetUserId();
        var transactionId = await sender.Send(
            new RecordTransactionCommand(ownerId, request.AccountId, request.Amount, request.BookedOn, request.Description, request.Note),
            cancellationToken);
        var transaction = await sender.Send(new GetTransactionQuery(ownerId, transactionId), cancellationToken);
        return TypedResults.Created($"/transactions/{transactionId}", transaction);
    }

    internal static async Task<Ok<TransactionDto>> GetTransactionAsync(
        Guid id, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
        => TypedResults.Ok(await sender.Send(new GetTransactionQuery(user.GetUserId(), id), cancellationToken));

    internal static async Task<Results<Accepted, NoContent>> ChangeTransactionAsync(
        Guid id, ChangeTransactionRequest request, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        var pending = await sender.Send(
            new ChangeTransactionCommand(user.GetUserId(), id, request.Amount, request.BookedOn, request.Description, request.Note),
            cancellationToken);
        return pending ? TypedResults.Accepted($"/transactions/{id}") : TypedResults.NoContent();
    }

    internal static async Task<Results<Accepted, NoContent>> DeleteTransactionAsync(
        Guid id, ClaimsPrincipal user, ISender sender, CancellationToken cancellationToken)
    {
        var pending = await sender.Send(new DeleteTransactionCommand(user.GetUserId(), id), cancellationToken);
        return pending ? TypedResults.Accepted($"/transactions/{id}") : TypedResults.NoContent();
    }
}

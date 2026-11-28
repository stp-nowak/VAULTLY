using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

using Vaultly.Core;
using Vaultly.Transactions.Application.Features.Transactions;
using Vaultly.Transactions.Application.Features.Transactions.GetTransactions;
using Vaultly.Transactions.Application.Features.Transactions.RecordTransaction;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions.Exceptions;

namespace Vaultly.Transactions.Api.Http;

/// <summary>
/// Answers rule violations as problem details: an unknown or foreign transaction or account is 404; a closed account,
/// a pending or rejected transaction and a concurrent change are 409; any other domain rule and an invalid page
/// request (cursor, limit) are 400. Everything else
/// stays a 500.
/// </summary>
internal sealed class TransactionsProblems(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int? status = exception switch
        {
            TransactionNotFoundException or AccountNotFoundException => StatusCodes.Status404NotFound,
            AccountClosedException or TransactionPendingException or TransactionRejectedException
                or DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            DomainException or InvalidPageRequestException => StatusCodes.Status400BadRequest,
            _ => null
        };

        if (status is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = status.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Detail = exception.Message }
        });
    }
}

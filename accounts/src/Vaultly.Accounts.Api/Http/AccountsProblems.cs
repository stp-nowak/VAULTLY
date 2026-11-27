using Microsoft.AspNetCore.Diagnostics;

using Vaultly.Accounts.Application.Common;
using Vaultly.Accounts.Domain.Exceptions;
using Vaultly.Core;

namespace Vaultly.Accounts.Api.Http;

/// <summary>
/// Answers rule violations as problem details: an unknown or foreign account is 404, a closed account 409 and any
/// other domain rule 400. Everything else stays a 500.
/// </summary>
internal sealed class AccountsProblems(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int? status = exception switch
        {
            AccountNotFoundException => StatusCodes.Status404NotFound,
            AccountClosedException => StatusCodes.Status409Conflict,
            DomainException => StatusCodes.Status400BadRequest,
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

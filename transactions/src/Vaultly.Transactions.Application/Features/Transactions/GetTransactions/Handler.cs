using Vaultly.Core.Messaging;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Transactions.GetTransactions;

public sealed class GetTransactionsHandler(ITransactionReadService readService)
    : IQueryHandler<GetTransactionsQuery, IReadOnlyList<TransactionDto>>
{
    public Task<IReadOnlyList<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
        => readService.GetTransactionsAsync(
            new OwnerId(request.OwnerId),
            request.AccountId is { } accountId ? new AccountId(accountId) : null,
            request.From,
            request.To,
            cancellationToken);
}

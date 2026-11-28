using Vaultly.Transactions.Application.Features.Transactions.GetTransactions;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Transactions;

public interface ITransactionReadService
{
    /// <summary>
    /// Up to <paramref name="limit"/> of the owner's transactions that are not deleted, newest day first and, within a
    /// day, newest recorded first; starting after <paramref name="after"/> when given.
    /// </summary>
    Task<TransactionPage> GetTransactionsAsync(
        OwnerId ownerId,
        AccountId? accountId,
        DateOnly? from,
        DateOnly? to,
        TransactionsCursor? after,
        int limit,
        CancellationToken cancellationToken);

    Task<TransactionDto?> GetTransactionAsync(TransactionId transactionId, OwnerId ownerId, CancellationToken cancellationToken);
}

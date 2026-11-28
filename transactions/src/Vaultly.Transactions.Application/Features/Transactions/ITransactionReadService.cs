using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Transactions;

public interface ITransactionReadService
{
    /// <summary>The owner's transactions that are not deleted, newest day first.</summary>
    Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(
        OwnerId ownerId, AccountId? accountId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken);

    Task<TransactionDto?> GetTransactionAsync(TransactionId transactionId, OwnerId ownerId, CancellationToken cancellationToken);
}

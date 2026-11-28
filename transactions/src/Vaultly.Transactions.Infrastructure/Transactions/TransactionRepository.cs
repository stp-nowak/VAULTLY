using Microsoft.EntityFrameworkCore;

using Vaultly.Transactions.Domain.Transactions;
using Vaultly.Transactions.Infrastructure.Persistence;

namespace Vaultly.Transactions.Infrastructure.Transactions;

public sealed class TransactionRepository(TransactionsDbContext dbContext) : ITransactionRepository
{
    public void Add(Transaction transaction) => dbContext.Transactions.Add(transaction);

    public Task<Transaction?> GetAsync(TransactionId transactionId, OwnerId ownerId, CancellationToken cancellationToken)
        => dbContext.Transactions.SingleOrDefaultAsync(
            x => x.Id == transactionId && x.OwnerId == ownerId && x.Status != TransactionStatus.Deleted,
            cancellationToken);

    public Task<Transaction?> GetAsync(TransactionId transactionId, CancellationToken cancellationToken)
        => dbContext.Transactions.SingleOrDefaultAsync(x => x.Id == transactionId, cancellationToken);
}

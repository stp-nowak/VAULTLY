namespace Vaultly.Transactions.Domain.Transactions;

public interface ITransactionRepository
{
    void Add(Transaction transaction);

    /// <summary>The transaction, or <c>null</c> when it does not exist, is deleted or belongs to another owner.</summary>
    Task<Transaction?> GetAsync(TransactionId transactionId, OwnerId ownerId, CancellationToken cancellationToken);

    /// <summary>The transaction whoever owns it, for answers from Accounts; <c>null</c> when unknown.</summary>
    Task<Transaction?> GetAsync(TransactionId transactionId, CancellationToken cancellationToken);
}

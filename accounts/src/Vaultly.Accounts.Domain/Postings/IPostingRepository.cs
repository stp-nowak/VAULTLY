namespace Vaultly.Accounts.Domain.Postings;

public interface IPostingRepository
{
    void Add(Posting posting);

    Task<Posting?> GetAsync(TransactionId transactionId, CancellationToken cancellationToken);
}

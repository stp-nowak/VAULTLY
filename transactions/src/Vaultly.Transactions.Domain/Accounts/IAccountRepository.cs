namespace Vaultly.Transactions.Domain.Accounts;

public interface IAccountRepository
{
    void Add(Account account);

    Task<Account?> GetAsync(AccountId accountId, CancellationToken cancellationToken);
}

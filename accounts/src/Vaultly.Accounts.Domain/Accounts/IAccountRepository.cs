namespace Vaultly.Accounts.Domain.Accounts;

public interface IAccountRepository
{
    void Add(Account account);

    /// <summary>The account, or <c>null</c> when it does not exist or belongs to another owner.</summary>
    Task<Account?> GetAsync(AccountId accountId, OwnerId ownerId, CancellationToken cancellationToken);
}

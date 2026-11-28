using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Infrastructure.Persistence;

namespace Vaultly.Transactions.Infrastructure.Accounts;

public sealed class AccountRepository(TransactionsDbContext dbContext) : IAccountRepository
{
    public void Add(Account account) => dbContext.Accounts.Add(account);

    public async Task<Account?> GetAsync(AccountId accountId, CancellationToken cancellationToken)
        => await dbContext.Accounts.FindAsync([accountId], cancellationToken);
}

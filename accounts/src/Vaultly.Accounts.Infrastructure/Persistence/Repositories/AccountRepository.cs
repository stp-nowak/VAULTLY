using Microsoft.EntityFrameworkCore;


using Vaultly.Accounts.Domain.Accounts;

namespace Vaultly.Accounts.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository(AccountsDbContext dbContext) : IAccountRepository
{
    public void Add(Account account) => dbContext.Accounts.Add(account);

    public Task<Account?> GetAsync(AccountId accountId, OwnerId ownerId, CancellationToken cancellationToken)
        => dbContext.Accounts.SingleOrDefaultAsync(x => x.Id == accountId && x.OwnerId == ownerId, cancellationToken);
}

using Microsoft.EntityFrameworkCore;

using Vaultly.Accounts.Application.Interfaces.Repositories;
using Vaultly.Accounts.Domain.Aggregates;
using Vaultly.Accounts.Domain.ValueObjects;

namespace Vaultly.Accounts.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository(AccountsDbContext dbContext) : IAccountRepository
{
    public void Add(Account account) => dbContext.Accounts.Add(account);

    public Task<Account?> GetAsync(AccountId accountId, OwnerId ownerId, CancellationToken cancellationToken)
        => dbContext.Accounts.SingleOrDefaultAsync(x => x.Id == accountId && x.OwnerId == ownerId, cancellationToken);
}

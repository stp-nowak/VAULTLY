using Microsoft.EntityFrameworkCore;

using Vaultly.Accounts.Domain.Postings;
using Vaultly.Accounts.Infrastructure.Persistence;

namespace Vaultly.Accounts.Infrastructure.Postings;

public sealed class PostingRepository(AccountsDbContext dbContext) : IPostingRepository
{
    public void Add(Posting posting) => dbContext.Postings.Add(posting);

    public Task<Posting?> GetAsync(TransactionId transactionId, CancellationToken cancellationToken)
        => dbContext.Postings.SingleOrDefaultAsync(x => x.Id == transactionId, cancellationToken);
}

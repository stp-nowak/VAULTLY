using Microsoft.EntityFrameworkCore;

using Vaultly.Accounts.Application.Features.Accounts.GetBalanceHistory;
using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Accounts.Infrastructure.Persistence;

namespace Vaultly.Accounts.Infrastructure.Postings;

public sealed class BalanceHistoryReadService(AccountsDbContext dbContext) : IBalanceHistoryReadService
{
    public async Task<BalanceMovements?> GetMovementsAsync(AccountId accountId, OwnerId ownerId, CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts.AsNoTracking()
            .Where(x => x.Id == accountId && x.OwnerId == ownerId)
            .Select(x => new { x.OpeningBalance })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null)
        {
            return null;
        }

        var totals = await dbContext.Postings.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .GroupBy(x => x.BookedOn)
            .Select(day => new { Date = day.Key, Amount = day.Sum(x => x.Amount) })
            .OrderBy(day => day.Date)
            .ToListAsync(cancellationToken);

        return new BalanceMovements(account.OpeningBalance, [.. totals.Select(day => new DailyTotal(day.Date, day.Amount))]);
    }
}

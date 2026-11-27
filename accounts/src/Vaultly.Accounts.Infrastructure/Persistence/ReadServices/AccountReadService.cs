using Microsoft.EntityFrameworkCore;

using Vaultly.Accounts.Application.Features.Accounts;
using Vaultly.Accounts.Application.Features.NetWorth.GetNetWorth;
using Vaultly.Accounts.Domain.Accounts;

namespace Vaultly.Accounts.Infrastructure.Persistence.ReadServices;

public sealed class AccountReadService(AccountsDbContext dbContext) : IAccountReadService, INetWorthReadService
{
    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(OwnerId ownerId, bool includeClosed, CancellationToken cancellationToken)
    {
        var accounts = await OwnedBy(ownerId)
            .Where(x => includeClosed || x.ClosedAt == null)
            .OrderBy(x => x.OpenedAt)
            .ToListAsync(cancellationToken);

        return [.. accounts.Select(ToDto)];
    }

    public async Task<AccountDto?> GetAccountAsync(AccountId accountId, OwnerId ownerId, CancellationToken cancellationToken)
    {
        var account = await OwnedBy(ownerId).SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken);
        return account is null ? null : ToDto(account);
    }

    public async Task<IReadOnlyList<CurrencyNetWorthDto>> GetNetWorthAsync(OwnerId ownerId, CancellationToken cancellationToken)
    {
        var totals = await OwnedBy(ownerId)
            .Where(x => x.ClosedAt == null)
            .GroupBy(x => x.Currency)
            .Select(group => new
            {
                Currency = group.Key,
                Assets = group.Sum(x => x.Balance > 0 ? x.Balance : 0),
                Liabilities = group.Sum(x => x.Balance < 0 ? -x.Balance : 0)
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. totals
                .Select(total => new CurrencyNetWorthDto(total.Currency.Code, total.Assets, total.Liabilities, total.Assets - total.Liabilities))
                .OrderBy(total => total.Currency, StringComparer.Ordinal)
        ];
    }

    private IQueryable<Account> OwnedBy(OwnerId ownerId)
        => dbContext.Accounts.AsNoTracking().Where(x => x.OwnerId == ownerId);

    private static AccountDto ToDto(Account account)
        => new(
            account.Id.Value,
            account.Name.Value,
            account.Type,
            account.Currency.Code,
            account.OpeningBalance,
            account.Balance,
            account.OpenedAt,
            account.ClosedAt);
}

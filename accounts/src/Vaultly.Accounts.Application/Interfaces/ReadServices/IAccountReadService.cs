using Vaultly.Accounts.Application.Features.Accounts;
using Vaultly.Accounts.Application.Features.NetWorth.GetNetWorth;
using Vaultly.Accounts.Domain.ValueObjects;

namespace Vaultly.Accounts.Application.Interfaces.ReadServices;

public interface IAccountReadService
{
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(OwnerId ownerId, bool includeClosed, CancellationToken cancellationToken);

    Task<AccountDto?> GetAccountAsync(AccountId accountId, OwnerId ownerId, CancellationToken cancellationToken);

    /// <summary>One entry per currency of the owner's open accounts, ordered by currency.</summary>
    Task<IReadOnlyList<CurrencyNetWorthDto>> GetNetWorthAsync(OwnerId ownerId, CancellationToken cancellationToken);
}

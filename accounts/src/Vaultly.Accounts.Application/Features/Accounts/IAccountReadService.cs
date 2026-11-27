using Vaultly.Accounts.Domain.Accounts;

namespace Vaultly.Accounts.Application.Features.Accounts;

public interface IAccountReadService
{
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(OwnerId ownerId, bool includeClosed, CancellationToken cancellationToken);

    Task<AccountDto?> GetAccountAsync(AccountId accountId, OwnerId ownerId, CancellationToken cancellationToken);
}

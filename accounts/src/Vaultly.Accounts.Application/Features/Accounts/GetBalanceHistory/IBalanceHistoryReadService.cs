using Vaultly.Accounts.Domain.Accounts;

namespace Vaultly.Accounts.Application.Features.Accounts.GetBalanceHistory;

public interface IBalanceHistoryReadService
{
    /// <summary><c>null</c> when the account does not exist or belongs to another owner.</summary>
    Task<BalanceMovements?> GetMovementsAsync(AccountId accountId, OwnerId ownerId, CancellationToken cancellationToken);
}

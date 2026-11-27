using Vaultly.Accounts.Domain.Accounts;

namespace Vaultly.Accounts.Application.Features.NetWorth.GetNetWorth;

public interface INetWorthReadService
{
    /// <summary>One entry per currency of the owner's open accounts, ordered by currency.</summary>
    Task<IReadOnlyList<CurrencyNetWorthDto>> GetNetWorthAsync(OwnerId ownerId, CancellationToken cancellationToken);
}

using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.NetWorth.GetNetWorth;

public sealed class GetNetWorthHandler(INetWorthReadService readService)
    : IQueryHandler<GetNetWorthQuery, IReadOnlyList<CurrencyNetWorthDto>>
{
    public Task<IReadOnlyList<CurrencyNetWorthDto>> Handle(GetNetWorthQuery request, CancellationToken cancellationToken)
        => readService.GetNetWorthAsync(new OwnerId(request.OwnerId), cancellationToken);
}

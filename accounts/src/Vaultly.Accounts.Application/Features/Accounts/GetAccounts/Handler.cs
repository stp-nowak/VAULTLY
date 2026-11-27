using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Accounts.GetAccounts;

public sealed class GetAccountsHandler(IAccountReadService readService) : IQueryHandler<GetAccountsQuery, IReadOnlyList<AccountDto>>
{
    public Task<IReadOnlyList<AccountDto>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
        => readService.GetAccountsAsync(new OwnerId(request.OwnerId), request.IncludeClosed, cancellationToken);
}

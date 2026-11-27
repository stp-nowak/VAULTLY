using Vaultly.Accounts.Application.Common;
using Vaultly.Accounts.Application.Interfaces.ReadServices;
using Vaultly.Accounts.Domain.ValueObjects;
using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Accounts.GetAccount;

public sealed class GetAccountHandler(IAccountReadService readService) : IQueryHandler<GetAccountQuery, AccountDto>
{
    public async Task<AccountDto> Handle(GetAccountQuery request, CancellationToken cancellationToken)
        => await readService.GetAccountAsync(new AccountId(request.AccountId), new OwnerId(request.OwnerId), cancellationToken)
           ?? throw new AccountNotFoundException(request.AccountId);
}

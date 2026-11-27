using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Core;
using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Accounts.RenameAccount;

public sealed class RenameAccountHandler(IAccountRepository accounts, IUnitOfWork unitOfWork)
    : ICommandHandler<RenameAccountCommand>
{
    public async Task Handle(RenameAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accounts.GetAsync(new AccountId(request.AccountId), new OwnerId(request.OwnerId), cancellationToken)
                      ?? throw new AccountNotFoundException(request.AccountId);

        account.Rename(new AccountName(request.Name));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

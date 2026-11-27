using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Core;
using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Accounts.CloseAccount;

public sealed class CloseAccountHandler(IAccountRepository accounts, IUnitOfWork unitOfWork, TimeProvider time)
    : ICommandHandler<CloseAccountCommand>
{
    public async Task Handle(CloseAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accounts.GetAsync(new AccountId(request.AccountId), new OwnerId(request.OwnerId), cancellationToken)
                      ?? throw new AccountNotFoundException(request.AccountId);

        account.Close(time.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

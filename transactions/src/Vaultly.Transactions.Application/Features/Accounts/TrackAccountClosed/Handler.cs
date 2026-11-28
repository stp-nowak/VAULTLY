using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Accounts.TrackAccountClosed;

public sealed class TrackAccountClosedHandler(IAccountRepository accounts, IUnitOfWork unitOfWork)
    : ICommandHandler<TrackAccountClosedCommand>
{
    public async Task Handle(TrackAccountClosedCommand request, CancellationToken cancellationToken)
    {
        var account = await accounts.GetAsync(new AccountId(request.AccountId), cancellationToken);
        if (account is null)
        {
            accounts.Add(Account.Closed(new AccountId(request.AccountId), new OwnerId(request.OwnerId), request.Version));
        }
        else
        {
            account.ApplyClosed(request.Version);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

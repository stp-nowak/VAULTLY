using Vaultly.Core;
using Vaultly.Core.Amounts;
using Vaultly.Core.Messaging;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Accounts.TrackAccountOpened;

public sealed class TrackAccountOpenedHandler(IAccountRepository accounts, IUnitOfWork unitOfWork)
    : ICommandHandler<TrackAccountOpenedCommand>
{
    public async Task Handle(TrackAccountOpenedCommand request, CancellationToken cancellationToken)
    {
        var currency = new Currency(request.Currency);
        var account = await accounts.GetAsync(new AccountId(request.AccountId), cancellationToken);
        if (account is null)
        {
            accounts.Add(Account.Opened(new AccountId(request.AccountId), new OwnerId(request.OwnerId), currency, request.Version));
        }
        else
        {
            account.ApplyOpened(currency, request.Version);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

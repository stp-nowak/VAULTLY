using Vaultly.Accounts.Application.Interfaces.Repositories;
using Vaultly.Accounts.Domain.Aggregates;
using Vaultly.Accounts.Domain.ValueObjects;
using Vaultly.Core;
using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Accounts.OpenAccount;

public sealed class OpenAccountHandler(IAccountRepository accounts, IUnitOfWork unitOfWork, TimeProvider time)
    : ICommandHandler<OpenAccountCommand, Guid>
{
    public async Task<Guid> Handle(OpenAccountCommand request, CancellationToken cancellationToken)
    {
        var account = Account.Open(
            new OwnerId(request.OwnerId),
            new AccountName(request.Name),
            request.Type,
            new Money(request.OpeningBalance, new Currency(request.Currency)),
            time.GetUtcNow());

        accounts.Add(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return account.Id.Value;
    }
}

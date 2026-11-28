using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Transactions.RecordTransaction;

public sealed class RecordTransactionHandler(
    IAccountRepository accounts,
    ITransactionRepository transactions,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<RecordTransactionCommand, Guid>
{
    public async Task<Guid> Handle(RecordTransactionCommand request, CancellationToken cancellationToken)
    {
        var account = await accounts.GetAsync(new AccountId(request.AccountId), cancellationToken);
        if (account is null || account.OwnerId != new OwnerId(request.OwnerId))
        {
            throw new AccountNotFoundException(request.AccountId);
        }

        var transaction = Transaction.Record(
            account,
            request.Amount,
            request.BookedOn,
            new TransactionDescription(request.Description),
            TransactionNote.From(request.Note),
            time.GetUtcNow());

        transactions.Add(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return transaction.Id.Value;
    }
}

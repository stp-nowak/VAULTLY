using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Transactions.ChangeTransaction;

public sealed class ChangeTransactionHandler(ITransactionRepository transactions, IUnitOfWork unitOfWork)
    : ICommandHandler<ChangeTransactionCommand, bool>
{
    public async Task<bool> Handle(ChangeTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await transactions.GetAsync(new TransactionId(request.TransactionId), new OwnerId(request.OwnerId), cancellationToken)
                          ?? throw new TransactionNotFoundException(request.TransactionId);

        var pending = transaction.Change(
            request.Amount,
            request.BookedOn,
            new TransactionDescription(request.Description),
            TransactionNote.From(request.Note));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return pending;
    }
}

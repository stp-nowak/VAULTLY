using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Transactions.DeleteTransaction;

public sealed class DeleteTransactionHandler(ITransactionRepository transactions, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteTransactionCommand, bool>
{
    public async Task<bool> Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await transactions.GetAsync(new TransactionId(request.TransactionId), new OwnerId(request.OwnerId), cancellationToken)
                          ?? throw new TransactionNotFoundException(request.TransactionId);

        var pending = transaction.Delete();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return pending;
    }
}

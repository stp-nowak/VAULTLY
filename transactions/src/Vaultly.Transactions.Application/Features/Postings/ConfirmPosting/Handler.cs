using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Postings.ConfirmPosting;

public sealed class ConfirmPostingHandler(ITransactionRepository transactions, IUnitOfWork unitOfWork)
    : ICommandHandler<ConfirmPostingCommand>
{
    public async Task Handle(ConfirmPostingCommand request, CancellationToken cancellationToken)
    {
        var transaction = await transactions.GetAsync(new TransactionId(request.TransactionId), cancellationToken);
        if (transaction is null)
        {
            return;
        }

        transaction.ConfirmPosting(request.StepId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

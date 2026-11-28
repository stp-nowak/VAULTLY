using Vaultly.Core;
using Vaultly.Core.Messaging;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Postings.DeclinePosting;

public sealed class DeclinePostingHandler(ITransactionRepository transactions, IUnitOfWork unitOfWork)
    : ICommandHandler<DeclinePostingCommand>
{
    public async Task Handle(DeclinePostingCommand request, CancellationToken cancellationToken)
    {
        var transaction = await transactions.GetAsync(new TransactionId(request.TransactionId), cancellationToken);
        if (transaction is null)
        {
            return;
        }

        transaction.Decline(request.StepId, request.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

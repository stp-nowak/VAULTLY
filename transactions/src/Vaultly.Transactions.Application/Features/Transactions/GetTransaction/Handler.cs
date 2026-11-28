using Vaultly.Core.Messaging;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Transactions.GetTransaction;

public sealed class GetTransactionHandler(ITransactionReadService readService) : IQueryHandler<GetTransactionQuery, TransactionDto>
{
    public async Task<TransactionDto> Handle(GetTransactionQuery request, CancellationToken cancellationToken)
        => await readService.GetTransactionAsync(new TransactionId(request.TransactionId), new OwnerId(request.OwnerId), cancellationToken)
           ?? throw new TransactionNotFoundException(request.TransactionId);
}

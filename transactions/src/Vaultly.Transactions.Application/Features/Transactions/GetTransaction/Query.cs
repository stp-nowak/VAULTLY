using Vaultly.Core.Messaging;

namespace Vaultly.Transactions.Application.Features.Transactions.GetTransaction;

public sealed record GetTransactionQuery(Guid OwnerId, Guid TransactionId) : IQuery<TransactionDto>;

using Vaultly.Core.Messaging;

namespace Vaultly.Transactions.Application.Features.Transactions.GetTransactions;

public sealed record GetTransactionsQuery(Guid OwnerId, Guid? AccountId, DateOnly? From, DateOnly? To)
    : IQuery<IReadOnlyList<TransactionDto>>;

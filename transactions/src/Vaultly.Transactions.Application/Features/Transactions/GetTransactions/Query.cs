using Vaultly.Core.Messaging;

namespace Vaultly.Transactions.Application.Features.Transactions.GetTransactions;

/// <summary>
/// One page of the owner's transactions, newest day first. <paramref name="Cursor"/> is the <c>NextCursor</c> of the
/// page before; <c>null</c> starts at the newest.
/// </summary>
public sealed record GetTransactionsQuery(
    Guid OwnerId,
    Guid? AccountId,
    DateOnly? From,
    DateOnly? To,
    string? Cursor,
    int? Limit) : IQuery<TransactionPageDto>;

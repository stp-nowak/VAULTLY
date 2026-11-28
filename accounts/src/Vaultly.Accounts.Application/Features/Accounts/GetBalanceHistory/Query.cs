using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Accounts.GetBalanceHistory;

/// <summary>The account's balance at the end of each day with a transaction, optionally between two days.</summary>
public sealed record GetBalanceHistoryQuery(Guid OwnerId, Guid AccountId, DateOnly? From, DateOnly? To)
    : IQuery<IReadOnlyList<DailyBalanceDto>>;

namespace Vaultly.Accounts.Application.Features.Accounts.GetBalanceHistory;

/// <summary>An account's opening balance and what its transactions add per day, oldest day first.</summary>
public sealed record BalanceMovements(decimal OpeningBalance, IReadOnlyList<DailyTotal> DailyTotals);

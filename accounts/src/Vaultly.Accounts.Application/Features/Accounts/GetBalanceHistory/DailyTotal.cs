namespace Vaultly.Accounts.Application.Features.Accounts.GetBalanceHistory;

public sealed record DailyTotal(DateOnly Date, decimal Amount);

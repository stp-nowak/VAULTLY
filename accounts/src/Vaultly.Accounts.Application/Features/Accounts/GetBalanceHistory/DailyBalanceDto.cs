namespace Vaultly.Accounts.Application.Features.Accounts.GetBalanceHistory;

public sealed record DailyBalanceDto(DateOnly Date, decimal Balance);

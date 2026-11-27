namespace Vaultly.Accounts.Application.Features.NetWorth.GetNetWorth;

/// <param name="Currency">ISO 4217 code.</param>
/// <param name="Assets">Sum of the positive balances.</param>
/// <param name="Liabilities">Sum of the debts, as a positive amount.</param>
/// <param name="NetWorth"><paramref name="Assets"/> minus <paramref name="Liabilities"/>.</param>
public sealed record CurrencyNetWorthDto(string Currency, decimal Assets, decimal Liabilities, decimal NetWorth);

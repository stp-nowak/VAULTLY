namespace Vaultly.Transactions.Domain.Transactions;

/// <summary>Why Accounts did not apply a step.</summary>
public enum DeclineReason
{
    AccountNotFound,
    AccountClosed,
    CurrencyMismatch
}

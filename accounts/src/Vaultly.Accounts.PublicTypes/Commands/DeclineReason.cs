namespace Vaultly.Accounts.PublicTypes.Commands;

public enum DeclineReason
{
    /// <summary>The account does not exist or belongs to another user.</summary>
    AccountNotFound,
    AccountClosed,
    /// <summary>The transaction is in another currency than the account.</summary>
    CurrencyMismatch
}

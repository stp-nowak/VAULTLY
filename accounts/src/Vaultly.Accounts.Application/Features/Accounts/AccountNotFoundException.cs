namespace Vaultly.Accounts.Application.Features.Accounts;

/// <summary>
/// The account does not exist or belongs to another user; both look the same so nobody learns which accounts exist.
/// </summary>
public sealed class AccountNotFoundException(Guid accountId) : Exception($"Account {accountId} was not found.");

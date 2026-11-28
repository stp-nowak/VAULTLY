namespace Vaultly.Transactions.Application.Features.Transactions.RecordTransaction;

/// <summary>
/// No account of the user has this id, or Transactions has not heard of it yet: it learns of new accounts from
/// Accounts' events, usually within a second.
/// </summary>
public sealed class AccountNotFoundException(Guid accountId) : Exception($"Account {accountId} was not found.");

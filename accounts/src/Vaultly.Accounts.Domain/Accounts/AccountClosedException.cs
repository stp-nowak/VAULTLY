using Vaultly.Core;

namespace Vaultly.Accounts.Domain.Accounts;

public sealed class AccountClosedException(AccountId accountId)
    : DomainException($"Account {accountId} is closed and cannot be changed.");

using Vaultly.Core;

namespace Vaultly.Transactions.Domain.Accounts;

public sealed class AccountClosedException(AccountId accountId)
    : DomainException($"Account {accountId} is closed; nothing can be booked on it.");

using Vaultly.Accounts.Domain.ValueObjects;
using Vaultly.Core;

namespace Vaultly.Accounts.Domain.Exceptions;

public sealed class InvalidValueException(string message) : DomainException(message);

public sealed class AccountClosedException(AccountId accountId)
    : DomainException($"Account {accountId} is closed and cannot be changed.");

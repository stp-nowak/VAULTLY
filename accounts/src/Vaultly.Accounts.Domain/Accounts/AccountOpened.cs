using Vaultly.Core;

namespace Vaultly.Accounts.Domain.Accounts;

public sealed record AccountOpened(AccountId AccountId, OwnerId OwnerId, AccountType Type, Currency Currency) : DomainEvent;

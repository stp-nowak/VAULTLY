using Vaultly.Accounts.Domain.Enums;
using Vaultly.Accounts.Domain.ValueObjects;
using Vaultly.Core;

namespace Vaultly.Accounts.Domain.Events;

public sealed record AccountOpened(AccountId AccountId, OwnerId OwnerId, AccountType Type, Currency Currency) : DomainEvent;

public sealed record AccountClosed(AccountId AccountId, OwnerId OwnerId) : DomainEvent;

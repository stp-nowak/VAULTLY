using Vaultly.Core;
using Vaultly.Core.Amounts;

namespace Vaultly.Accounts.Domain.Accounts.Events;

public sealed record AccountOpened(AccountId AccountId, OwnerId OwnerId, AccountType Type, Currency Currency) : DomainEvent;

using Vaultly.Core;

namespace Vaultly.Accounts.Domain.Accounts.Events;

public sealed record AccountClosed(AccountId AccountId, OwnerId OwnerId) : DomainEvent;

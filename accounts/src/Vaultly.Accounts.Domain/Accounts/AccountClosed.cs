using Vaultly.Core;

namespace Vaultly.Accounts.Domain.Accounts;

public sealed record AccountClosed(AccountId AccountId, OwnerId OwnerId) : DomainEvent;

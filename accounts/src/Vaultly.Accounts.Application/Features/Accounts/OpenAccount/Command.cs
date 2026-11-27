using Vaultly.Accounts.Domain.Enums;
using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Accounts.OpenAccount;

/// <summary>Opens a manual account; returns its id.</summary>
public sealed record OpenAccountCommand(Guid OwnerId, string Name, AccountType Type, string Currency, decimal OpeningBalance)
    : ICommand<Guid>;

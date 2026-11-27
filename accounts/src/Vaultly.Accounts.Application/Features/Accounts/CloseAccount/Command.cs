using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Accounts.CloseAccount;

public sealed record CloseAccountCommand(Guid OwnerId, Guid AccountId) : ICommand;

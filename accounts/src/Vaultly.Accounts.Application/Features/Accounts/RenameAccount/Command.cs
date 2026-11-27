using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Accounts.RenameAccount;

public sealed record RenameAccountCommand(Guid OwnerId, Guid AccountId, string Name) : ICommand;

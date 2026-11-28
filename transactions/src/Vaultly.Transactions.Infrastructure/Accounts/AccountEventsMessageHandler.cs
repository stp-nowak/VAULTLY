using MediatR;

using Rebus.Handlers;

using Vaultly.Accounts.PublicTypes.Events;
using Vaultly.Transactions.Application.Features.Accounts.TrackAccountClosed;
using Vaultly.Transactions.Application.Features.Accounts.TrackAccountOpened;

namespace Vaultly.Transactions.Infrastructure.Accounts;

/// <summary>Keeps the copy of the accounts from the Accounts service's events.</summary>
public sealed class AccountEventsMessageHandler(ISender sender) : IHandleMessages<AccountOpened>, IHandleMessages<AccountClosed>
{
    public Task Handle(AccountOpened message)
        => sender.Send(new TrackAccountOpenedCommand(message.AccountId, message.OwnerId, message.Currency, message.Version));

    public Task Handle(AccountClosed message)
        => sender.Send(new TrackAccountClosedCommand(message.AccountId, message.OwnerId, message.Version));
}

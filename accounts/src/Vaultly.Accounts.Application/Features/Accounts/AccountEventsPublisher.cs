using MediatR;

using Vaultly.Accounts.Domain.Accounts.Events;
using Vaultly.Core.Messaging;

using Published = Vaultly.Accounts.PublicTypes.Events;

namespace Vaultly.Accounts.Application.Features.Accounts;

/// <summary>Tells other services when an account opens or closes, e.g. so Transactions knows where it may book.</summary>
public sealed class AccountEventsPublisher(IMessageBus bus)
    : INotificationHandler<AccountOpened>, INotificationHandler<AccountClosed>
{
    public Task Handle(AccountOpened notification, CancellationToken cancellationToken)
        => bus.PublishAsync(new Published.AccountOpened(
            notification.AccountId.Value,
            notification.OwnerId.Value,
            notification.Currency.Code,
            notification.Version));

    public Task Handle(AccountClosed notification, CancellationToken cancellationToken)
        => bus.PublishAsync(new Published.AccountClosed(
            notification.AccountId.Value,
            notification.OwnerId.Value,
            notification.Version));
}

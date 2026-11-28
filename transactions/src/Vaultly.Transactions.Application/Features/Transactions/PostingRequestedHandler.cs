using MediatR;

using Vaultly.Core.Messaging;
using Vaultly.Transactions.Application.Features.Postings;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Transactions;

/// <summary>Starts the posting saga for a step, in the same unit of work as the transaction it belongs to.</summary>
public sealed class PostingRequestedHandler(IMessageBus bus) : INotificationHandler<PostingRequested>
{
    public Task Handle(PostingRequested notification, CancellationToken cancellationToken)
        => bus.SendLocalAsync(new StartPosting(
            notification.StepId,
            notification.TransactionId.Value,
            notification.AccountId.Value,
            notification.OwnerId.Value,
            notification.Step,
            notification.Amount,
            notification.Currency.Code,
            notification.BookedOn));
}

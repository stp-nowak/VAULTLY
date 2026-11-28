using MediatR;

using Rebus.Bus;
using Rebus.Handlers;

using Vaultly.Accounts.Application.Features.Postings.PostTransaction;
using Vaultly.Accounts.PublicTypes.Commands;

namespace Vaultly.Accounts.Infrastructure.Postings;

/// <summary>Takes <see cref="PostTransaction"/> from the bus and answers the Transactions service.</summary>
public sealed class PostTransactionMessageHandler(ISender sender, IBus bus) : IHandleMessages<PostTransaction>
{
    public async Task Handle(PostTransaction message)
    {
        var declined = await sender.Send(new PostTransactionCommand(
            message.StepId,
            message.TransactionId,
            message.AccountId,
            message.OwnerId,
            message.Operation,
            message.Amount,
            message.Currency,
            message.BookedOn));

        await bus.Reply(declined is { } reason
            ? new TransactionDeclined(message.StepId, message.TransactionId, reason)
            : new TransactionPosted(message.StepId, message.TransactionId));
    }
}

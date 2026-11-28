using MediatR;

using Microsoft.Extensions.Logging;

using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Sagas;

using Vaultly.Accounts.PublicTypes.Commands;
using Vaultly.Transactions.Application.Features.Postings;
using Vaultly.Transactions.Application.Features.Postings.ConfirmPosting;
using Vaultly.Transactions.Application.Features.Postings.DeclinePosting;
using Vaultly.Transactions.Domain.Transactions;

using AccountsDeclineReason = Vaultly.Accounts.PublicTypes.Commands.DeclineReason;
using DeclineReason = Vaultly.Transactions.Domain.Transactions.DeclineReason;

namespace Vaultly.Transactions.Infrastructure.Postings;

/// <summary>
/// Orchestrates one step of a transaction (recording, changing or deleting it) with the Accounts service: sends it
/// <see cref="PostTransaction"/>, waits for <see cref="TransactionPosted"/> or <see cref="TransactionDeclined"/> and
/// hands the answer to the transaction, which confirms or undoes the step. Without an answer it sends the command
/// again (Accounts applies a step only once) up to <see cref="MaxAttempts"/> times.
/// </summary>
public sealed class TransactionPostingSaga(IBus bus, ISender sender, ILogger<TransactionPostingSaga> logger)
    : Saga<PostingSagaData>,
        IAmInitiatedBy<StartPosting>,
        IHandleMessages<TransactionPosted>,
        IHandleMessages<TransactionDeclined>,
        IHandleMessages<PostingTimedOut>
{
    public const int MaxAttempts = 5;

    public static readonly TimeSpan AnswerTimeout = TimeSpan.FromMinutes(1);

    protected override void CorrelateMessages(ICorrelationConfig<PostingSagaData> config)
    {
        config.Correlate<StartPosting>(message => message.StepId, data => data.StepId);
        config.Correlate<TransactionPosted>(message => message.StepId, data => data.StepId);
        config.Correlate<TransactionDeclined>(message => message.StepId, data => data.StepId);
        config.Correlate<PostingTimedOut>(message => message.StepId, data => data.StepId);
    }

    public async Task Handle(StartPosting message)
    {
        if (!IsNew)
        {
            return;
        }

        Data.TransactionId = message.TransactionId;
        Data.AccountId = message.AccountId;
        Data.OwnerId = message.OwnerId;
        Data.Step = message.Step;
        Data.Amount = message.Amount;
        Data.Currency = message.Currency;
        Data.BookedOn = message.BookedOn;
        await SendToAccountsAsync();
    }

    public async Task Handle(TransactionPosted message)
    {
        await sender.Send(new ConfirmPostingCommand(message.TransactionId, message.StepId));
        MarkAsComplete();
    }

    public async Task Handle(TransactionDeclined message)
    {
        await sender.Send(new DeclinePostingCommand(message.TransactionId, message.StepId, ToDomain(message.Reason)));
        MarkAsComplete();
    }

    public async Task Handle(PostingTimedOut message)
    {
        if (Data.Attempts >= MaxAttempts)
        {
            logger.LogError(
                "Accounts answered none of {Attempts} requests to post step {StepId} of transaction {TransactionId}; it stays pending",
                Data.Attempts, Data.StepId, Data.TransactionId);
            return;
        }

        await SendToAccountsAsync();
    }

    private async Task SendToAccountsAsync()
    {
        Data.Attempts++;
        await bus.Send(new PostTransaction(
            Data.StepId,
            Data.TransactionId,
            Data.AccountId,
            Data.OwnerId,
            ToContract(Data.Step),
            Data.Amount,
            Data.Currency,
            Data.BookedOn));
        await bus.DeferLocal(AnswerTimeout, new PostingTimedOut(Data.StepId));
    }

    private static PostingOperation ToContract(PostingStep step) => step switch
    {
        PostingStep.Record => PostingOperation.Record,
        PostingStep.Change => PostingOperation.Change,
        PostingStep.Delete => PostingOperation.Delete,
        _ => throw new ArgumentOutOfRangeException(nameof(step), step, null)
    };

    private static DeclineReason ToDomain(AccountsDeclineReason reason) => reason switch
    {
        AccountsDeclineReason.AccountNotFound => DeclineReason.AccountNotFound,
        AccountsDeclineReason.AccountClosed => DeclineReason.AccountClosed,
        AccountsDeclineReason.CurrencyMismatch => DeclineReason.CurrencyMismatch,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
    };
}

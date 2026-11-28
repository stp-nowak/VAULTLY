using Vaultly.Accounts.PublicTypes.Commands;
using Vaultly.Core.Messaging;

namespace Vaultly.Accounts.Application.Features.Postings.PostTransaction;

/// <summary>Applies one step of a transaction to its account's balance; returns why it was declined, or <c>null</c>.</summary>
public sealed record PostTransactionCommand(
    Guid StepId,
    Guid TransactionId,
    Guid AccountId,
    Guid OwnerId,
    PostingOperation Operation,
    decimal Amount,
    string Currency,
    DateOnly BookedOn) : ICommand<DeclineReason?>;

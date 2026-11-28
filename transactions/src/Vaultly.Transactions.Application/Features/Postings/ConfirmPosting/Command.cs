using Vaultly.Core.Messaging;

namespace Vaultly.Transactions.Application.Features.Postings.ConfirmPosting;

/// <summary>Accounts applied a step of the transaction to the balance.</summary>
public sealed record ConfirmPostingCommand(Guid TransactionId, Guid StepId) : ICommand;

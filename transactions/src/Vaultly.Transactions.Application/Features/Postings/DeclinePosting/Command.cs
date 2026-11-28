using Vaultly.Core.Messaging;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Postings.DeclinePosting;

/// <summary>Accounts declined a step of the transaction; the step is undone.</summary>
public sealed record DeclinePostingCommand(Guid TransactionId, Guid StepId, DeclineReason Reason) : ICommand;

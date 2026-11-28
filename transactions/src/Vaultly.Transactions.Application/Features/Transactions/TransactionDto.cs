using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Features.Transactions;

public sealed record TransactionDto(
    Guid Id,
    Guid AccountId,
    decimal Amount,
    string Currency,
    DateOnly BookedOn,
    string Description,
    string? Note,
    TransactionStatus Status,
    PostingStep? PendingStep,
    DeclineReason? DeclineReason,
    DateTimeOffset RecordedAt);

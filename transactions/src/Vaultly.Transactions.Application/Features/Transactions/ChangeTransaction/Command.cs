using Vaultly.Core.Messaging;

namespace Vaultly.Transactions.Application.Features.Transactions.ChangeTransaction;

/// <summary>Changes a transaction; returns <c>true</c> when the change waits for Accounts (amount or date changed).</summary>
public sealed record ChangeTransactionCommand(
    Guid OwnerId,
    Guid TransactionId,
    decimal Amount,
    DateOnly BookedOn,
    string Description,
    string? Note) : ICommand<bool>;

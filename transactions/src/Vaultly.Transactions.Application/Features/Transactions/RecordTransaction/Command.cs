using Vaultly.Core.Messaging;

namespace Vaultly.Transactions.Application.Features.Transactions.RecordTransaction;

/// <summary>Records a transaction on one of the owner's open accounts; returns its id. It stays pending until Accounts
/// applies it to the balance.</summary>
public sealed record RecordTransactionCommand(
    Guid OwnerId,
    Guid AccountId,
    decimal Amount,
    DateOnly BookedOn,
    string Description,
    string? Note) : ICommand<Guid>;

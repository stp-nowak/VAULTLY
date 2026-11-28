using Vaultly.Core.Messaging;

namespace Vaultly.Transactions.Application.Features.Transactions.DeleteTransaction;

/// <summary>Deletes a transaction; returns <c>true</c> when the deletion waits for Accounts to take it off the balance.</summary>
public sealed record DeleteTransactionCommand(Guid OwnerId, Guid TransactionId) : ICommand<bool>;

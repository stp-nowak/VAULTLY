namespace Vaultly.Transactions.Application.Features.Transactions;

/// <summary>
/// The transaction does not exist, was deleted or belongs to another user; all look the same so nobody learns which
/// transactions exist.
/// </summary>
public sealed class TransactionNotFoundException(Guid transactionId) : Exception($"Transaction {transactionId} was not found.");

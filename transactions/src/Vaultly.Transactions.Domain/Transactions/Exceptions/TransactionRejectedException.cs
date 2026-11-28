using Vaultly.Core;

namespace Vaultly.Transactions.Domain.Transactions.Exceptions;

public sealed class TransactionRejectedException(TransactionId transactionId)
    : DomainException($"Transaction {transactionId} was rejected; it can only be deleted.");

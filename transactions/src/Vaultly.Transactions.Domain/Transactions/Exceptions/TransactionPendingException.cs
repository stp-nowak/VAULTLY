using Vaultly.Core;

namespace Vaultly.Transactions.Domain.Transactions.Exceptions;

public sealed class TransactionPendingException(TransactionId transactionId)
    : DomainException($"Transaction {transactionId} waits for its account to apply the last change; try again shortly.");

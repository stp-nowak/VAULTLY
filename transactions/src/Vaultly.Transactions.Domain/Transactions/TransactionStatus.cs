namespace Vaultly.Transactions.Domain.Transactions;

public enum TransactionStatus
{
    /// <summary>A step (recording, changing or deleting) waits for Accounts to apply it to the balance.</summary>
    Pending,

    /// <summary>The account's balance reflects the transaction.</summary>
    Posted,

    /// <summary>Accounts declined recording it; it never touched the balance.</summary>
    Rejected,

    Deleted
}

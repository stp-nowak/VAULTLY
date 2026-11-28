namespace Vaultly.Transactions.Application.Features.Transactions.GetTransactions;

/// <summary>A page of transactions; <paramref name="Next"/> is where the next page starts, <c>null</c> on the last.</summary>
public sealed record TransactionPage(IReadOnlyList<TransactionDto> Items, TransactionsCursor? Next);

namespace Vaultly.Transactions.Application.Features.Transactions.GetTransactions;

/// <summary>A page of transactions; <paramref name="NextCursor"/> is <c>null</c> on the last page.</summary>
public sealed record TransactionPageDto(IReadOnlyList<TransactionDto> Items, string? NextCursor);

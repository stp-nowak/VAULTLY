namespace Vaultly.Transactions.Api.Contracts;

public sealed record RecordTransactionRequest(Guid AccountId, decimal Amount, DateOnly BookedOn, string Description, string? Note);

public sealed record ChangeTransactionRequest(decimal Amount, DateOnly BookedOn, string Description, string? Note);

namespace Vaultly.Accounts.PublicTypes.Commands;

/// <summary>Reply to <see cref="PostTransaction"/>: the account's balance reflects the step.</summary>
public sealed record TransactionPosted(Guid StepId, Guid TransactionId);

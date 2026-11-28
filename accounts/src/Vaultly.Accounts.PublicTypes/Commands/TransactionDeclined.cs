namespace Vaultly.Accounts.PublicTypes.Commands;

/// <summary>Reply to <see cref="PostTransaction"/>: the step was not applied and the balance did not change.</summary>
public sealed record TransactionDeclined(Guid StepId, Guid TransactionId, DeclineReason Reason);

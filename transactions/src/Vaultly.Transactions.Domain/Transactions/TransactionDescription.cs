using Vaultly.Core;

namespace Vaultly.Transactions.Domain.Transactions;

/// <summary>What the transaction was, e.g. the shop or the person paid.</summary>
public sealed record TransactionDescription
{
    public const int MaxLength = 200;

    public TransactionDescription(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new InvalidValueException("Transaction description is required.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"Transaction description must be {MaxLength} characters or fewer.");
        }

        Value = trimmed;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

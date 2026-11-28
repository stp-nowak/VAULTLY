using Vaultly.Core;

namespace Vaultly.Transactions.Domain.Transactions;

public sealed record TransactionNote
{
    public const int MaxLength = 1000;

    private TransactionNote(string value) => Value = value;

    public string Value { get; }

    /// <summary>The note, or <c>null</c> when <paramref name="value"/> is blank.</summary>
    public static TransactionNote? From(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"Transaction note must be {MaxLength} characters or fewer.");
        }

        return new TransactionNote(trimmed);
    }

    public override string ToString() => Value;
}

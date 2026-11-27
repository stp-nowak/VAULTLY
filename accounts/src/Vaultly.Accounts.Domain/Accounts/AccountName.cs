namespace Vaultly.Accounts.Domain.Accounts;

public sealed record AccountName
{
    public const int MaxLength = 100;

    public AccountName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw new InvalidValueException("Account name is required.");
        }

        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"Account name must be {MaxLength} characters or fewer.");
        }

        Value = trimmed;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

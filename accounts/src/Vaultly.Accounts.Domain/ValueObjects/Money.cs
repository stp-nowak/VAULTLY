using Vaultly.Accounts.Domain.Exceptions;

namespace Vaultly.Accounts.Domain.ValueObjects;

/// <summary>A signed amount in one currency; a debt is negative.</summary>
public sealed record Money
{
    public const int Precision = 19;
    public const int Scale = 4;

    private const decimal Limit = 1_000_000_000_000_000m;

    public Money(decimal amount, Currency currency)
    {
        if (decimal.Round(amount, Scale) != amount)
        {
            throw new InvalidValueException($"Amounts have at most {Scale} decimal places.");
        }

        if (Math.Abs(amount) >= Limit)
        {
            throw new InvalidValueException("Amount is too large.");
        }

        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public Currency Currency { get; }
}

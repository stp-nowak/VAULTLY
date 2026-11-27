using System.Collections.Frozen;
using System.Globalization;

namespace Vaultly.Accounts.Domain.Accounts;

/// <summary>An ISO 4217 currency code that some country uses, e.g. <c>PLN</c>.</summary>
public sealed record Currency
{
    public const int Length = 3;

    private static readonly FrozenSet<string> IsoCodes = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
        .Select(culture => new RegionInfo(culture.Name).ISOCurrencySymbol)
        .ToFrozenSet(StringComparer.Ordinal);

    public Currency(string? code)
    {
        var normalized = code?.Trim().ToUpperInvariant();
        if (normalized is null || !IsoCodes.Contains(normalized))
        {
            throw new InvalidValueException($"'{code}' is not an ISO 4217 currency code.");
        }

        Code = normalized;
    }

    public string Code { get; }

    public override string ToString() => Code;
}

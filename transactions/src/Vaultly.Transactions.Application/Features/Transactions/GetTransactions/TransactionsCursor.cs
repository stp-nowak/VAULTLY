using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Vaultly.Transactions.Application.Features.Transactions.GetTransactions;

/// <summary>
/// The position after the last transaction of a page: its day and its place in the order transactions were recorded
/// (unique, so transactions of the same day keep a stable order). Opaque to clients.
/// </summary>
public readonly record struct TransactionsCursor(DateOnly BookedOn, long Sequence)
{
    private const string DayFormat = "yyyy-MM-dd";

    public string Encode()
        => Base64Url.EncodeToString(Encoding.UTF8.GetBytes(
            $"{BookedOn.ToString(DayFormat, CultureInfo.InvariantCulture)}:{Sequence.ToString(CultureInfo.InvariantCulture)}"));

    public static bool TryDecode(string value, out TransactionsCursor cursor)
    {
        cursor = default;
        string text;
        try
        {
            text = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(value));
        }
        catch (FormatException)
        {
            return false;
        }

        var parts = text.Split(':');
        if (parts.Length != 2
            || !DateOnly.TryParseExact(parts[0], DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var bookedOn)
            || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence))
        {
            return false;
        }

        cursor = new TransactionsCursor(bookedOn, sequence);
        return true;
    }
}

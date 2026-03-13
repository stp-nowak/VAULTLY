using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record PersonName
{
    public const int MaxLength = 200;

    /// <summary>
    /// Creates a new person name value object.
    /// </summary>
    public PersonName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidValueException("Name is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new InvalidValueException($"Name must be {MaxLength} characters or fewer.");
        }

        Value = trimmed;
    }

    /// <summary>
    /// Gets the name value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the name as a string.
    /// </summary>
    public override string ToString() => Value;
}
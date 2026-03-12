using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

public sealed record PersonName
{
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
        if (trimmed.Length > 200)
        {
            throw new InvalidValueException("Name must be 200 characters or fewer.");
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
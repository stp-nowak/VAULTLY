using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Domain.ValueObjects;

/// <summary>A one-way, salted hash of a user's password in the hasher's own format.</summary>
public sealed record PasswordHash
{
    public const int MaxLength = 512;

    public PasswordHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
        {
            throw new InvalidValueException("Password hash is invalid.");
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => "[password hash]";
}

namespace Vaultly.Identity.Domain.Users;

/// <summary>
/// A password a person typed, checked against the password policy. It is only ever hashed (see
/// <see cref="PasswordHash"/>), never stored, and never shown by <see cref="ToString"/>.
/// </summary>
public sealed record Password
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    /// <exception cref="WeakPasswordException">Shorter than <see cref="MinLength"/>, longer than <see cref="MaxLength"/> or blank.</exception>
    public Password(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < MinLength or > MaxLength)
        {
            throw new WeakPasswordException();
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => "[password]";
}

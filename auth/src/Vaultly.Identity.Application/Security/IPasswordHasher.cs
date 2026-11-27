using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Application.Security;

public enum PasswordVerification
{
    Failed,
    Succeeded,

    /// <summary>Correct, but hashed with outdated parameters: store a new hash.</summary>
    SucceededRehashNeeded,
}

public interface IPasswordHasher
{
    PasswordHash Hash(Password password);

    PasswordVerification Verify(PasswordHash hash, Password password);

    /// <summary>
    /// Takes as long as <see cref="Verify"/> without a stored hash, so a failed sign-in for an unknown email or an
    /// account without a password cannot be told apart by timing.
    /// </summary>
    void VerifyWithoutAccount(Password password);
}

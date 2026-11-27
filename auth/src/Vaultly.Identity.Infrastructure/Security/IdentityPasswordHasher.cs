using Microsoft.AspNetCore.Identity;

using Vaultly.Identity.Application.Security;
using Vaultly.Identity.Domain.Users.Passwords;

namespace Vaultly.Identity.Infrastructure.Security;

/// <summary>
/// Password hashing with ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/> (PBKDF2, per-password salt, format
/// versioned so old hashes are upgraded on sign-in).
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly PasswordOwner Owner = new();

    private readonly PasswordHasher<PasswordOwner> _hasher = new();

    // A real hash of a random password, verified when there is no account so the work done is the same.
    private readonly string _withoutAccountHash;

    public IdentityPasswordHasher()
    {
        _withoutAccountHash = _hasher.HashPassword(Owner, Guid.NewGuid().ToString("N"));
    }

    public PasswordHash Hash(Password password) => new(_hasher.HashPassword(Owner, password.Value));

    public PasswordVerification Verify(PasswordHash hash, Password password)
        => _hasher.VerifyHashedPassword(Owner, hash.Value, password.Value) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Succeeded,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SucceededRehashNeeded,
            _ => PasswordVerification.Failed,
        };

    public void VerifyWithoutAccount(Password password) => _hasher.VerifyHashedPassword(Owner, _withoutAccountHash, password.Value);

    // PasswordHasher<TUser> needs a user type but does not use it.
    private sealed class PasswordOwner;
}

using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Application.Interfaces.Services;

public interface IJwtTokenService
{
    /// <summary>
    /// Creates a signed access token for the provided user and session.
    /// </summary>
    string CreateAccessToken(User user, Session session, DateTimeOffset now);
}

public interface IJwtKeyProvider
{
    /// <summary>
    /// Gets the public JWK representation for the signing key.
    /// </summary>
    JwkDto GetJwk();
}

public sealed record JwkDto(string Kty, string Use, string Alg, string Kid, string N, string E);

public interface ITokenGenerator
{
    /// <summary>
    /// Generates a random token of the requested length.
    /// </summary>
    string GenerateToken(int length);
}

public interface ITokenHasher
{
    /// <summary>
    /// Hashes the provided token using a stable hashing strategy.
    /// </summary>
    string HashToken(string token);
}

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

public interface IClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
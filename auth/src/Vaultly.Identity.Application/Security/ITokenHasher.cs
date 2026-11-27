namespace Vaultly.Identity.Application.Security;

public interface ITokenHasher
{
    /// <summary>
    /// Hashes the provided token using a stable hashing strategy.
    /// </summary>
    string HashToken(string token);
}

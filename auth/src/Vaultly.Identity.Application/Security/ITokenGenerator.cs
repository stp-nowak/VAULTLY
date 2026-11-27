namespace Vaultly.Identity.Application.Security;

public interface ITokenGenerator
{
    /// <summary>
    /// Generates a random token of the requested length.
    /// </summary>
    string GenerateToken(int length);
}

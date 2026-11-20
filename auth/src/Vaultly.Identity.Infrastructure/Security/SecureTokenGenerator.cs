using System.Security.Cryptography;

using Microsoft.IdentityModel.Tokens;

using Vaultly.Identity.Application.Interfaces.Services;

namespace Vaultly.Identity.Infrastructure.Security;

public sealed class SecureTokenGenerator : ITokenGenerator
{
    /// <summary>
    /// Generates a cryptographically secure random token.
    /// </summary>
    public string GenerateToken(int length)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return Base64UrlEncoder.Encode(bytes);
    }
}
using System.Security.Cryptography;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using Vaultly.Identity.Application.Interfaces.Services;

namespace Vaultly.Identity.Infrastructure.Security;

public sealed class Sha256TokenHasher : ITokenHasher
{
    /// <summary>
    /// Hashes the provided token with SHA-256 and base64url encoding.
    /// </summary>
    public string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Base64UrlEncoder.Encode(hash);
    }
}
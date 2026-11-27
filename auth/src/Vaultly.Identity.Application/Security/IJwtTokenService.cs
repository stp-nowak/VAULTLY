using Vaultly.Identity.Domain.Sessions;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Application.Security;

public interface IJwtTokenService
{
    /// <summary>
    /// Creates a signed access token for the provided user and session.
    /// </summary>
    string CreateAccessToken(User user, Session session, DateTimeOffset now);
}

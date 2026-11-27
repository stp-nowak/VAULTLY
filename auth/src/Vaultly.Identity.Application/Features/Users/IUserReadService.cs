using Vaultly.Identity.Application.Features.Users.GetConsoleUser;
using Vaultly.Identity.Application.Features.Users.GetUsers;

namespace Vaultly.Identity.Application.Features.Users;

/// <summary>
/// Read side (CQRS) for users, used by the admin console.
/// </summary>
public interface IUserReadService
{
    /// <summary>
    /// Gets the user of the SSO session with this token hash, or <c>null</c> when there is no such session or it is
    /// revoked or expired at <paramref name="now"/>.
    /// </summary>
    Task<ConsoleUserDto?> GetBySsoTokenHashAsync(string ssoTokenHash, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Gets one page of users, newest first, optionally only those whose email or name contains <paramref name="search"/>
    /// (case-insensitive).
    /// </summary>
    /// <param name="page">1-based page number.</param>
    Task<UserPageDto> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
}

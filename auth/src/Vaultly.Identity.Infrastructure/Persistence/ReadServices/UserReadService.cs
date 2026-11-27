using Microsoft.EntityFrameworkCore;

using Vaultly.Identity.Application.Features.Users.GetConsoleUser;
using Vaultly.Identity.Application.Features.Users.GetUsers;
using Vaultly.Identity.Application.Interfaces.ReadServices;
using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Infrastructure.Persistence.ReadServices;

/// <summary>
/// EF Core implementation of the user read side (admin console). <c>AsNoTracking</c> projections only.
/// </summary>
public sealed class UserReadService(IdentityDbContext dbContext) : IUserReadService
{
    private readonly IdentityDbContext _dbContext = dbContext;

    public async Task<ConsoleUserDto?> GetBySsoTokenHashAsync(string ssoTokenHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var hash = new TokenHash(ssoTokenHash);

        return await (
                from session in _dbContext.Sessions.AsNoTracking()
                join user in _dbContext.Users.AsNoTracking() on session.UserId equals user.Id
                where session.SsoTokenHash == hash && !session.IsRevoked && session.SsoExpiresAt > now
                select new ConsoleUserDto(user.Id.Value, user.Email.Value, user.Name.Value, user.Role))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UserPageDto> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var users = Search(_dbContext.Users.AsNoTracking(), search);
        var total = await users.CountAsync(cancellationToken);

        var rows = await users
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.Name,
                x.Role,
                x.EmailVerified,
                x.CreatedAt,
                Providers = x.ExternalIdentities.Select(e => e.ProviderId).Distinct().ToList(),
                HasPassword = x.PasswordHash != null,
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x => new UserSummaryDto(
                x.Id.Value,
                x.Email.Value,
                x.Name.Value,
                x.Role,
                x.EmailVerified,
                x.CreatedAt,
                SignInMethods(x.Providers, x.HasPassword)))
            .ToList();

        return new UserPageDto(items, total, page, pageSize);
    }

    private static List<string> SignInMethods(IEnumerable<ProviderId> providers, bool hasPassword)
        => [.. providers.Select(x => x.Value), .. hasPassword ? [SignInMethod.Password.Value] : Array.Empty<string>()];

    // The explicit string casts of the value objects translate to the plain columns (`lower("Email"::text) LIKE …`),
    // so the filter runs in the database. `.Value` would not translate, and EF.Property<string> makes EF convert the
    // search term with the value object's converter.
    internal static IQueryable<User> Search(IQueryable<User> users, string? search)
    {
        if (search is null)
        {
            return users;
        }

        var term = search.ToLowerInvariant();
        return users.Where(x => ((string)x.Email).ToLower().Contains(term) || ((string)x.Name).ToLower().Contains(term));
    }
}

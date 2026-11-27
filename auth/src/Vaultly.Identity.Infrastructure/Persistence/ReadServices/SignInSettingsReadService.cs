using Microsoft.EntityFrameworkCore;


using Vaultly.Identity.Application.Features.SignInMethods;
using Vaultly.Identity.Domain.SignInMethods;

namespace Vaultly.Identity.Infrastructure.Persistence.ReadServices;

public sealed class SignInSettingsReadService(IdentityDbContext dbContext) : ISignInSettingsReadService
{
    private readonly IdentityDbContext _dbContext = dbContext;

    public async Task<IReadOnlySet<string>> GetDisabledMethodsAsync(CancellationToken cancellationToken)
    {
        var disabled = await _dbContext.SignInSettings
            .AsNoTracking()
            .Where(x => x.Id == SignInSettings.SingletonId)
            .Select(x => EF.Property<List<string>>(x, "DisabledMethodValues"))
            .SingleOrDefaultAsync(cancellationToken);

        return (disabled ?? []).ToHashSet(StringComparer.Ordinal);
    }
}

using Microsoft.EntityFrameworkCore;


using Vaultly.Identity.Domain.SignInMethods;
using Vaultly.Identity.Infrastructure.Persistence;

namespace Vaultly.Identity.Infrastructure.SignInMethods;

public sealed class SignInSettingsRepository(IdentityDbContext dbContext) : ISignInSettingsRepository
{
    private readonly IdentityDbContext _dbContext = dbContext;

    public Task<SignInSettings?> FindAsync(CancellationToken cancellationToken)
        => _dbContext.SignInSettings.SingleOrDefaultAsync(x => x.Id == SignInSettings.SingletonId, cancellationToken);

    public void Add(SignInSettings settings) => _dbContext.SignInSettings.Add(settings);
}

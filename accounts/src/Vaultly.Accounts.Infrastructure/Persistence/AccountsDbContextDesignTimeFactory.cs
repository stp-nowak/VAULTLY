using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vaultly.Accounts.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> build the model without starting the API or a database.</summary>
public sealed class AccountsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
    public AccountsDbContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<AccountsDbContext>().UseNpgsql().Options);
}

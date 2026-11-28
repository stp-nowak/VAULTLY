using Microsoft.EntityFrameworkCore;

using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Accounts.Domain.Postings;
using Vaultly.Core;

namespace Vaultly.Accounts.Infrastructure.Persistence;

public sealed class AccountsDbContext(DbContextOptions<AccountsDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Posting> Postings => Set<Posting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountsDbContext).Assembly);
    }
}

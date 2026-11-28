using Microsoft.EntityFrameworkCore;

using Vaultly.Core;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Infrastructure.Persistence;

public sealed class TransactionsDbContext(DbContextOptions<TransactionsDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionsDbContext).Assembly);
    }
}

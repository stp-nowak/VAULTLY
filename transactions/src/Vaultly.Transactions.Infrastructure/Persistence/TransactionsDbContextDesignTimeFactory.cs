using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vaultly.Transactions.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> build the model without starting the API or a database.</summary>
public sealed class TransactionsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TransactionsDbContext>
{
    public TransactionsDbContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<TransactionsDbContext>().UseNpgsql().Options);
}

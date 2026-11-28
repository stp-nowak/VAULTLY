using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Vaultly.Core.Amounts;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Infrastructure.Accounts;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> entity)
    {
        entity.Ignore(x => x.DomainEvents);
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .ValueGeneratedNever();
        entity.Property(x => x.OwnerId)
            .HasConversion(id => id.Value, value => new OwnerId(value));
        entity.Property(x => x.Currency)
            .HasConversion(currency => currency!.Code, value => new Currency(value))
            .HasMaxLength(Currency.Length)
            .IsFixedLength();
        entity.Property(x => x.Version).IsConcurrencyToken();
    }
}

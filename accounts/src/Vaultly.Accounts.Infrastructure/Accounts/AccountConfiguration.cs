using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Core.Amounts;
using Vaultly.Core.Persistence;

namespace Vaultly.Accounts.Infrastructure.Accounts;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    private const int AccountTypeMaxLength = 20;

    public void Configure(EntityTypeBuilder<Account> entity)
    {
        entity.Ignore(x => x.DomainEvents);
        entity.Ignore(x => x.IsClosed);
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new AccountId(value))
            .ValueGeneratedNever();
        entity.Property(x => x.OwnerId)
            .HasConversion(id => id.Value, value => new OwnerId(value));
        entity.Property(x => x.Name)
            .HasConversion(name => name.Value, value => new AccountName(value))
            .HasMaxLength(AccountName.MaxLength)
            .IsRequired();
        entity.Property(x => x.Type)
            .HasConversion(new WireNameConverter<AccountType>())
            .HasMaxLength(AccountTypeMaxLength)
            .IsRequired();
        entity.Property(x => x.Currency)
            .HasConversion(currency => currency.Code, value => new Currency(value))
            .HasMaxLength(Currency.Length)
            .IsFixedLength()
            .IsRequired();
        entity.Property(x => x.OpeningBalance).HasPrecision(Money.Precision, Money.Scale);
        entity.Property(x => x.Balance).HasPrecision(Money.Precision, Money.Scale);
        entity.Property(x => x.Version).IsConcurrencyToken();
        entity.HasIndex(x => x.OwnerId);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Accounts.Domain.Postings;
using Vaultly.Core.Amounts;

namespace Vaultly.Accounts.Infrastructure.Postings;

internal sealed class PostingConfiguration : IEntityTypeConfiguration<Posting>
{
    public void Configure(EntityTypeBuilder<Posting> entity)
    {
        entity.Ignore(x => x.DomainEvents);
        entity.Ignore(x => x.Effect);
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TransactionId(value))
            .ValueGeneratedNever();
        entity.Property(x => x.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value));
        entity.Property(x => x.Amount).HasPrecision(Money.Precision, Money.Scale);
        entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId);
        entity.HasIndex(x => new { x.AccountId, x.BookedOn });
    }
}

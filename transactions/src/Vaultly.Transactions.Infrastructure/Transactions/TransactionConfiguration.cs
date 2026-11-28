using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Vaultly.Core.Amounts;
using Vaultly.Core.Persistence;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Infrastructure.Transactions;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    private const int EnumMaxLength = 20;

    public void Configure(EntityTypeBuilder<Transaction> entity)
    {
        entity.Ignore(x => x.DomainEvents);
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new TransactionId(value))
            .ValueGeneratedNever();
        entity.Property(x => x.OwnerId)
            .HasConversion(id => id.Value, value => new OwnerId(value));
        entity.Property(x => x.AccountId)
            .HasConversion(id => id.Value, value => new AccountId(value));
        entity.Property(x => x.Amount).HasPrecision(Money.Precision, Money.Scale);
        entity.Property(x => x.PostedAmount).HasPrecision(Money.Precision, Money.Scale);
        entity.Property(x => x.Currency)
            .HasConversion(currency => currency.Code, value => new Currency(value))
            .HasMaxLength(Currency.Length)
            .IsFixedLength()
            .IsRequired();
        entity.Property(x => x.Description)
            .HasConversion(description => description.Value, value => new TransactionDescription(value))
            .HasMaxLength(TransactionDescription.MaxLength)
            .IsRequired();
        entity.Property(x => x.Note)
            .HasConversion(note => note!.Value, value => TransactionNote.From(value))
            .HasMaxLength(TransactionNote.MaxLength);
        entity.Property(x => x.Status)
            .HasConversion(new WireNameConverter<TransactionStatus>())
            .HasMaxLength(EnumMaxLength)
            .IsRequired();
        entity.Property(x => x.PendingStep)
            .HasConversion(new WireNameConverter<PostingStep>())
            .HasMaxLength(EnumMaxLength);
        entity.Property(x => x.DeclineReason)
            .HasConversion(new WireNameConverter<DeclineReason>())
            .HasMaxLength(EnumMaxLength);
        entity.Property(x => x.Version).IsConcurrencyToken();
        entity.HasIndex(x => new { x.OwnerId, x.BookedOn });
        entity.HasIndex(x => x.AccountId);
    }
}

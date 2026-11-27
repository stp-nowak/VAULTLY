using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Vaultly.Core.Persistence;
using Vaultly.Identity.Domain.Users;
using Vaultly.Identity.Domain.Users.Passwords;
using Vaultly.Identity.PublicTypes;

namespace Vaultly.Identity.Infrastructure.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    private const int RoleMaxLength = 20;

    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.Ignore(x => x.DomainEvents);
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id)
            .HasConversion(id => id.Value, value => UserId.From(value));
        entity.Property(x => x.Email)
            .HasConversion(email => email.Value, value => new EmailAddress(value))
            .HasMaxLength(EmailAddress.MaxLength)
            .IsRequired();
        entity.Property(x => x.Name)
            .HasConversion(name => name.Value, value => new PersonName(value))
            .HasMaxLength(PersonName.MaxLength)
            .IsRequired();
        entity.Property(x => x.Role)
            .HasConversion(new WireNameConverter<UserRole>())
            .HasMaxLength(RoleMaxLength)
            .IsRequired();
        entity.Ignore(x => x.IsAdmin);
        entity.Property(x => x.PasswordHash)
            .HasConversion(hash => hash!.Value, value => new PasswordHash(value))
            .HasMaxLength(PasswordHash.MaxLength);
        entity.Ignore(x => x.HasPassword);
        entity.HasIndex(x => x.Email).IsUnique();
        entity.HasMany(x => x.ExternalIdentities)
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.Navigation(x => x.ExternalIdentities).HasField("_externalIdentities");
    }
}

using Vaultly.Identity.Domain.Entities;
using Vaultly.Identity.Domain.Events;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.SharedKernel;

namespace Vaultly.Identity.Domain.Aggregates;

public sealed class User : AggregateRoot
{
    private readonly List<ExternalIdentity> _externalIdentities = new();

    /// <summary>
    /// Required by EF Core.
    /// </summary>
    private User()
    {
    }

    public UserId Id { get; private set; }
    public EmailAddress Email { get; private set; } = null!;
    public PersonName Name { get; private set; } = null!;
    public bool EmailVerified { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<ExternalIdentity> ExternalIdentities => _externalIdentities.AsReadOnly();

    /// <summary>
    /// Creates a new user aggregate with the required invariants enforced.
    /// </summary>
    public static User Create(EmailAddress email, PersonName name, bool emailVerified, DateTimeOffset now)
    {
        var user = new User
        {
            Id = UserId.New(),
            Email = email,
            Name = name,
            EmailVerified = emailVerified,
            CreatedAt = now
        };

        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email, user.Name, user.EmailVerified));
        return user;
    }

    /// <summary>
    /// Updates the user profile details.
    /// </summary>
    public void UpdateProfile(PersonName name, bool emailVerified)
    {
        var changed = Name != name || EmailVerified != emailVerified;
        Name = name;
        EmailVerified = emailVerified;

        if (changed)
        {
            RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id, Name, EmailVerified));
        }
    }

    /// <summary>
    /// Links a new external identity to the user.
    /// </summary>
    public ExternalIdentity LinkExternalIdentity(
        ProviderId providerId,
        ProviderUserId providerUserId,
        EmailAddress email,
        PersonName name,
        DateTimeOffset now)
    {
        if (_externalIdentities.Any(x => x.ProviderId == providerId && x.ProviderUserId == providerUserId))
        {
            throw new DuplicateExternalIdentityException(providerId.Value, providerUserId.Value);
        }

        var identity = ExternalIdentity.Create(Id, providerId, providerUserId, email, name, now);
        _externalIdentities.Add(identity);
        RaiseDomainEvent(new ExternalIdentityLinkedDomainEvent(Id, identity.Id, providerId, providerUserId));
        return identity;
    }
}
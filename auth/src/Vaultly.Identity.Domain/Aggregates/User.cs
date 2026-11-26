using Vaultly.Identity.Domain.Entities;
using Vaultly.Identity.Domain.Enums;
using Vaultly.Identity.Domain.Events;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;
using Vaultly.Identity.SharedKernel;

namespace Vaultly.Identity.Domain.Aggregates;

public sealed class User : AggregateRoot
{
    /// <summary>Failed password sign-ins in a row that pause password sign-in for <see cref="LockoutDuration"/>.</summary>
    public const int MaxFailedSignIns = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

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
    public UserRole Role { get; private set; }
    public PasswordHash? PasswordHash { get; private set; }
    public int FailedSignInCount { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

    public bool IsAdmin => Role == UserRole.Admin;
    public bool HasPassword => PasswordHash is not null;

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
            CreatedAt = now,
            Role = UserRole.User
        };

        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email, user.Name, user.EmailVerified));
        return user;
    }

    /// <summary>
    /// Registers a user who signs in with email and password. The email is not verified: nobody has proven they own it.
    /// </summary>
    public static User RegisterWithPassword(EmailAddress email, PersonName name, PasswordHash passwordHash, DateTimeOffset now)
    {
        var user = Create(email, name, emailVerified: false, now);
        user.PasswordHash = passwordHash;
        return user;
    }

    public bool IsLockedOut(DateTimeOffset now) => LockedUntil > now;

    /// <summary>Counts a wrong password; the <see cref="MaxFailedSignIns"/>th in a row pauses password sign-in.</summary>
    public void RecordFailedSignIn(DateTimeOffset now)
    {
        FailedSignInCount++;
        if (FailedSignInCount < MaxFailedSignIns)
        {
            return;
        }

        FailedSignInCount = 0;
        LockedUntil = now + LockoutDuration;
        RaiseDomainEvent(new UserLockedOutDomainEvent(Id, LockedUntil.Value));
    }

    public void RecordSuccessfulSignIn()
    {
        FailedSignInCount = 0;
        LockedUntil = null;
    }

    public void SetPassword(PasswordHash passwordHash)
    {
        PasswordHash = passwordHash;
        RaiseDomainEvent(new UserPasswordSetDomainEvent(Id));
    }

    /// <summary>Stores the same password hashed with newer parameters (no change for the user).</summary>
    public void UpgradePasswordHash(PasswordHash passwordHash)
    {
        if (HasPassword)
        {
            PasswordHash = passwordHash;
        }
    }

    public void ClearPassword()
    {
        if (!HasPassword)
        {
            return;
        }

        PasswordHash = null;
        FailedSignInCount = 0;
        LockedUntil = null;
        RaiseDomainEvent(new UserPasswordRemovedDomainEvent(Id));
    }

    /// <summary>
    /// A provider that verified the email signs in to this account. If the email was not verified yet (the account was
    /// registered with a password nobody confirmed), the password is removed, because whoever set it may not own the
    /// email. Returns whether that happened, so the caller can also end the account's sessions.
    /// </summary>
    public bool SecureWithVerifiedEmail()
    {
        if (EmailVerified)
        {
            return false;
        }

        ClearPassword();
        EmailVerified = true;
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id, Name, EmailVerified));
        return true;
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
    /// Gives the user another role. Only <see cref="Services.RoleAssignment"/> calls it, because whether an admin
    /// may lose the role depends on the other users.
    /// </summary>
    internal void ChangeRole(UserRole role)
    {
        if (Role == role)
        {
            return;
        }

        var previousRole = Role;
        Role = role;
        RaiseDomainEvent(new UserRoleChangedDomainEvent(Id, previousRole, role));
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
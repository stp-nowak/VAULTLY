using NUnit.Framework;

using Vaultly.Identity.Domain.Users;
using Vaultly.Identity.Domain.Users.Roles;
using Vaultly.Identity.PublicTypes;

namespace Vaultly.Identity.Domain.Tests;

[TestFixture]
public sealed class RoleAssignmentTests
{
    private static readonly DateTimeOffset Now = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void NewUser_IsARegularUser()
    {
        var user = CreateUser();

        Assert.Multiple(() =>
        {
            Assert.That(user.Role, Is.EqualTo(UserRole.User));
            Assert.That(user.IsAdmin, Is.False);
        });
    }

    [Test]
    public void Assign_WhenPromotingToAdmin_ChangesRoleAndRaisesEvent()
    {
        var user = CreateUser();
        user.ClearDomainEvents();

        RoleAssignment.Assign(user, UserRole.Admin, adminCount: 0);

        var domainEvent = user.DomainEvents.OfType<UserRoleChangedDomainEvent>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(user.IsAdmin, Is.True);
            Assert.That(domainEvent.UserId, Is.EqualTo(user.Id));
            Assert.That(domainEvent.PreviousRole, Is.EqualTo(UserRole.User));
            Assert.That(domainEvent.Role, Is.EqualTo(UserRole.Admin));
        });
    }

    [Test]
    public void Assign_WhenDemotingOneOfSeveralAdmins_ChangesRole()
    {
        var admin = CreateAdmin();

        RoleAssignment.Assign(admin, UserRole.User, adminCount: 2);

        Assert.That(admin.Role, Is.EqualTo(UserRole.User));
    }

    [Test]
    public void Assign_WhenDemotingTheLastAdmin_ThrowsAndKeepsRole()
    {
        var admin = CreateAdmin();

        Assert.Multiple(() =>
        {
            Assert.That(() => RoleAssignment.Assign(admin, UserRole.User, adminCount: 1), Throws.TypeOf<LastAdminException>());
            Assert.That(admin.IsAdmin, Is.True);
        });
    }

    [Test]
    public void Assign_WhenRoleUnchanged_RaisesNoEvent()
    {
        var admin = CreateAdmin();
        admin.ClearDomainEvents();

        RoleAssignment.Assign(admin, UserRole.Admin, adminCount: 1);

        Assert.That(admin.DomainEvents, Is.Empty);
    }

    private static User CreateUser()
        => User.Create(new EmailAddress("user@example.com"), new PersonName("Test User"), true, Now);

    private static User CreateAdmin()
    {
        var user = CreateUser();
        RoleAssignment.Assign(user, UserRole.Admin, adminCount: 0);
        return user;
    }
}

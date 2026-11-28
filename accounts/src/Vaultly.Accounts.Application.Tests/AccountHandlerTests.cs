using Moq;

using NUnit.Framework;

using Vaultly.Accounts.Application.Features.Accounts;
using Vaultly.Accounts.Application.Features.Accounts.CloseAccount;
using Vaultly.Accounts.Application.Features.Accounts.OpenAccount;
using Vaultly.Accounts.Application.Features.Accounts.RenameAccount;
using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Core;
using Vaultly.Core.Amounts;

namespace Vaultly.Accounts.Application.Tests;

[TestFixture]
public sealed class AccountHandlerTests
{
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    private Mock<IAccountRepository> _accounts = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private FixedTime _time = null!;

    [SetUp]
    public void SetUp()
    {
        _accounts = new Mock<IAccountRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _time = new FixedTime(Now);
    }

    [Test]
    public async Task OpenAccount_AddsTheAccount_SavesIt_AndReturnsItsId()
    {
        Account? added = null;
        _accounts.Setup(x => x.Add(It.IsAny<Account>())).Callback<Account>(account => added = account);
        var handler = new OpenAccountHandler(_accounts.Object, _unitOfWork.Object, _time);

        var id = await handler.Handle(new OpenAccountCommand(OwnerId, "Wallet", AccountType.Cash, "pln", 50m), CancellationToken.None);

        Assert.That(added, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(id, Is.EqualTo(added!.Id.Value));
            Assert.That(added.OwnerId.Value, Is.EqualTo(OwnerId));
            Assert.That(added.Currency.Code, Is.EqualTo("PLN"));
            Assert.That(added.OpenedAt, Is.EqualTo(Now));
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void OpenAccount_WithInvalidCurrency_ThrowsWithoutSaving()
    {
        var handler = new OpenAccountHandler(_accounts.Object, _unitOfWork.Object, _time);

        Assert.ThrowsAsync<InvalidValueException>(() =>
            handler.Handle(new OpenAccountCommand(OwnerId, "Wallet", AccountType.Cash, "XYZ", 0m), CancellationToken.None));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RenameAccount_RenamesTheOwnersAccount()
    {
        var account = ExistingAccount();
        var handler = new RenameAccountHandler(_accounts.Object, _unitOfWork.Object);

        await handler.Handle(new RenameAccountCommand(OwnerId, account.Id.Value, "Pocket money"), CancellationToken.None);

        Assert.That(account.Name.Value, Is.EqualTo("Pocket money"));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void RenameAccount_WhenTheOwnerHasNoSuchAccount_ThrowsNotFound()
    {
        var handler = new RenameAccountHandler(_accounts.Object, _unitOfWork.Object);

        Assert.ThrowsAsync<AccountNotFoundException>(() =>
            handler.Handle(new RenameAccountCommand(OwnerId, Guid.NewGuid(), "Other"), CancellationToken.None));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CloseAccount_ClosesItNow()
    {
        var account = ExistingAccount();
        var handler = new CloseAccountHandler(_accounts.Object, _unitOfWork.Object, _time);

        await handler.Handle(new CloseAccountCommand(OwnerId, account.Id.Value), CancellationToken.None);

        Assert.That(account.ClosedAt, Is.EqualTo(Now));
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CloseAccount_WhenTheOwnerHasNoSuchAccount_ThrowsNotFound()
    {
        var handler = new CloseAccountHandler(_accounts.Object, _unitOfWork.Object, _time);

        Assert.ThrowsAsync<AccountNotFoundException>(() =>
            handler.Handle(new CloseAccountCommand(OwnerId, Guid.NewGuid()), CancellationToken.None));
    }

    private Account ExistingAccount()
    {
        var account = Account.Open(
            new OwnerId(OwnerId), new AccountName("Wallet"), AccountType.Cash, new Money(10m, new Currency("PLN")), Now.AddDays(-1));
        _accounts.Setup(x => x.GetAsync(account.Id, new OwnerId(OwnerId), It.IsAny<CancellationToken>())).ReturnsAsync(account);
        return account;
    }
}

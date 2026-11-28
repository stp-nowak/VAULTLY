using Moq;

using NUnit.Framework;

using Vaultly.Core;
using Vaultly.Core.Amounts;
using Vaultly.Core.Testing;
using Vaultly.Transactions.Application.Features.Accounts.TrackAccountClosed;
using Vaultly.Transactions.Application.Features.Accounts.TrackAccountOpened;
using Vaultly.Transactions.Application.Features.Postings;
using Vaultly.Transactions.Application.Features.Postings.ConfirmPosting;
using Vaultly.Transactions.Application.Features.Postings.DeclinePosting;
using Vaultly.Transactions.Application.Features.Transactions;
using Vaultly.Transactions.Application.Features.Transactions.ChangeTransaction;
using Vaultly.Transactions.Application.Features.Transactions.DeleteTransaction;
using Vaultly.Transactions.Application.Features.Transactions.RecordTransaction;
using Vaultly.Transactions.Domain.Accounts;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Tests;

[TestFixture]
public sealed class TransactionHandlerTests
{
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AccountGuid = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateOnly Day = new(2026, 11, 20);
    private static readonly DateTimeOffset Now = new(2026, 11, 27, 12, 0, 0, TimeSpan.Zero);

    private Account? _account;
    private Transaction? _transaction;
    private Mock<IAccountRepository> _accounts = null!;
    private Mock<ITransactionRepository> _transactions = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;

    [SetUp]
    public void SetUp()
    {
        _account = Account.Opened(new AccountId(AccountGuid), new OwnerId(Owner), new Currency("PLN"), 1);
        _transaction = null;
        _accounts = new Mock<IAccountRepository>();
        _accounts.Setup(x => x.GetAsync(new AccountId(AccountGuid), It.IsAny<CancellationToken>())).ReturnsAsync(() => _account);
        _accounts.Setup(x => x.Add(It.IsAny<Account>())).Callback<Account>(account => _account = account);
        _transactions = new Mock<ITransactionRepository>();
        _transactions.Setup(x => x.Add(It.IsAny<Transaction>())).Callback<Transaction>(transaction => _transaction = transaction);
        _transactions.Setup(x => x.GetAsync(It.IsAny<TransactionId>(), new OwnerId(Owner), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _transaction);
        _transactions.Setup(x => x.GetAsync(It.IsAny<TransactionId>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => _transaction);
        _unitOfWork = new Mock<IUnitOfWork>();
    }

    [Test]
    public async Task Record_OnTheOwnersAccount_AddsAPendingTransaction()
    {
        var id = await Record();

        Assert.Multiple(() =>
        {
            Assert.That(_transaction!.Id.Value, Is.EqualTo(id));
            Assert.That(_transaction.Status, Is.EqualTo(TransactionStatus.Pending));
            Assert.That(_transaction.RecordedAt, Is.EqualTo(Now));
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Record_OnAnUnknownOrForeignAccount_IsNotFound()
    {
        var handler = new RecordTransactionHandler(_accounts.Object, _transactions.Object, _unitOfWork.Object, new FixedTime(Now));

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<AccountNotFoundException>(() => handler.Handle(
                new RecordTransactionCommand(Guid.NewGuid(), AccountGuid, -50m, Day, "Shop", null), CancellationToken.None));
            Assert.ThrowsAsync<AccountNotFoundException>(() => handler.Handle(
                new RecordTransactionCommand(Owner, Guid.NewGuid(), -50m, Day, "Shop", null), CancellationToken.None));
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ChangeAndDelete_SayWhetherTheyWaitForAccounts()
    {
        await Record();
        await Confirm();
        var change = new ChangeTransactionHandler(_transactions.Object, _unitOfWork.Object);
        var delete = new DeleteTransactionHandler(_transactions.Object, _unitOfWork.Object);

        var descriptionOnly = await change.Handle(new ChangeTransactionCommand(Owner, _transaction!.Id.Value, -50m, Day, "Groceries", null), CancellationToken.None);
        var newAmount = await change.Handle(new ChangeTransactionCommand(Owner, _transaction.Id.Value, -80m, Day, "Groceries", null), CancellationToken.None);
        await Confirm();
        var deletion = await delete.Handle(new DeleteTransactionCommand(Owner, _transaction.Id.Value), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(descriptionOnly, Is.False);
            Assert.That(newAmount, Is.True);
            Assert.That(deletion, Is.True);
        });
    }

    [Test]
    public void ChangingAnUnknownTransaction_IsNotFound()
    {
        var change = new ChangeTransactionHandler(_transactions.Object, _unitOfWork.Object);

        Assert.ThrowsAsync<TransactionNotFoundException>(() =>
            change.Handle(new ChangeTransactionCommand(Owner, Guid.NewGuid(), -50m, Day, "Shop", null), CancellationToken.None));
    }

    [Test]
    public async Task DeclinePosting_UndoesTheStep()
    {
        await Record();
        var decline = new DeclinePostingHandler(_transactions.Object, _unitOfWork.Object);

        await decline.Handle(
            new DeclinePostingCommand(_transaction!.Id.Value, _transaction.PendingStepId!.Value, DeclineReason.AccountClosed),
            CancellationToken.None);

        Assert.That(_transaction.Status, Is.EqualTo(TransactionStatus.Rejected));
    }

    [Test]
    public async Task AnswersForAnUnknownTransaction_AreIgnored()
    {
        var confirm = new ConfirmPostingHandler(_transactions.Object, _unitOfWork.Object);

        await confirm.Handle(new ConfirmPostingCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PostingRequested_StartsTheSaga_ThroughThisServicesQueue()
    {
        await Record();
        var bus = new RecordingMessageBus();
        var requested = (PostingRequested)_transaction!.DomainEvents.Single();

        await new PostingRequestedHandler(bus).Handle(requested, CancellationToken.None);

        Assert.That(bus.Messages.Single(), Is.EqualTo(new StartPosting(
            requested.StepId, _transaction.Id.Value, AccountGuid, Owner, PostingStep.Record, -50m, "PLN", Day)));
    }

    [Test]
    public async Task AccountEvents_KeepTheCopy_AndIgnoreOlderOnes()
    {
        _account = null;
        var opened = new TrackAccountOpenedHandler(_accounts.Object, _unitOfWork.Object);
        var closed = new TrackAccountClosedHandler(_accounts.Object, _unitOfWork.Object);

        await opened.Handle(new TrackAccountOpenedCommand(AccountGuid, Owner, "eur", 1), CancellationToken.None);
        await closed.Handle(new TrackAccountClosedCommand(AccountGuid, Owner, 4), CancellationToken.None);
        await opened.Handle(new TrackAccountOpenedCommand(AccountGuid, Owner, "eur", 1), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(_account!.Currency?.Code, Is.EqualTo("EUR"));
            Assert.That(_account.IsClosed, Is.True);
            Assert.That(_account.Version, Is.EqualTo(4));
        });
    }

    private async Task<Guid> Record()
    {
        var handler = new RecordTransactionHandler(_accounts.Object, _transactions.Object, _unitOfWork.Object, new FixedTime(Now));
        return await handler.Handle(new RecordTransactionCommand(Owner, AccountGuid, -50m, Day, "Shop", null), CancellationToken.None);
    }

    private Task Confirm()
        => new ConfirmPostingHandler(_transactions.Object, _unitOfWork.Object).Handle(
            new ConfirmPostingCommand(_transaction!.Id.Value, _transaction.PendingStepId!.Value), CancellationToken.None);
}

using Moq;

using NUnit.Framework;

using Rebus.TestHelpers;
using Rebus.TestHelpers.Events;

using Vaultly.Accounts.Application.Features.Accounts;
using Vaultly.Accounts.Application.Features.Accounts.GetBalanceHistory;
using Vaultly.Accounts.Application.Features.Postings.PostTransaction;
using Vaultly.Accounts.Domain.Accounts;
using Vaultly.Accounts.Domain.Postings;
using Vaultly.Accounts.Infrastructure.Postings;
using Vaultly.Accounts.PublicTypes.Commands;
using Vaultly.Core;
using Vaultly.Core.Amounts;

using MediatR;

namespace Vaultly.Accounts.Application.Tests;

[TestFixture]
public sealed class PostingHandlerTests
{
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Transaction = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateOnly Day = new(2026, 11, 20);

    private Account _account = null!;
    private Posting? _posting;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private PostTransactionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _account = Account.Open(new OwnerId(Owner), new AccountName("Wallet"), AccountType.Cash,
            new Money(100m, new Currency("PLN")), DateTimeOffset.UnixEpoch);
        _posting = null;

        var accounts = new Mock<IAccountRepository>();
        accounts.Setup(x => x.GetAsync(_account.Id, new OwnerId(Owner), It.IsAny<CancellationToken>())).ReturnsAsync(() => _account);
        var postings = new Mock<IPostingRepository>();
        postings.Setup(x => x.GetAsync(new TransactionId(Transaction), It.IsAny<CancellationToken>())).ReturnsAsync(() => _posting);
        postings.Setup(x => x.Add(It.IsAny<Posting>())).Callback<Posting>(posting => _posting = posting);
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new PostTransactionHandler(accounts.Object, postings.Object, _unitOfWork.Object);
    }

    [Test]
    public async Task Record_AddsTheAmountToTheBalance()
    {
        var declined = await Post(PostingOperation.Record, -50m);

        Assert.Multiple(() =>
        {
            Assert.That(declined, Is.Null);
            Assert.That(_account.Balance, Is.EqualTo(50m));
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task TheSameStepAgain_ChangesNothing_AndIsStillPosted()
    {
        var step = Guid.NewGuid();
        await Post(PostingOperation.Record, -50m, step);

        var declined = await Post(PostingOperation.Record, -50m, step);

        Assert.Multiple(() =>
        {
            Assert.That(declined, Is.Null);
            Assert.That(_account.Balance, Is.EqualTo(50m));
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Change_ReplacesTheEarlierAmount()
    {
        await Post(PostingOperation.Record, -50m);

        await Post(PostingOperation.Change, -80m);

        Assert.That(_account.Balance, Is.EqualTo(20m));
    }

    [Test]
    public async Task Delete_TakesTheAmountBack()
    {
        await Post(PostingOperation.Record, -50m);

        await Post(PostingOperation.Delete, 0m);

        Assert.Multiple(() =>
        {
            Assert.That(_account.Balance, Is.EqualTo(100m));
            Assert.That(_posting!.IsDeleted, Is.True);
        });
    }

    [Test]
    public async Task UnknownOrForeignAccount_IsDeclined()
    {
        var declined = await _handler.Handle(
            new PostTransactionCommand(Guid.NewGuid(), Transaction, _account.Id.Value, Guid.NewGuid(), PostingOperation.Record, -50m, "PLN", Day),
            CancellationToken.None);

        Assert.That(declined, Is.EqualTo(DeclineReason.AccountNotFound));
    }

    [Test]
    public async Task ClosedAccount_IsDeclined_WithoutChangingTheBalance()
    {
        _account.Close(DateTimeOffset.UnixEpoch);

        var declined = await Post(PostingOperation.Record, -50m);

        Assert.Multiple(() =>
        {
            Assert.That(declined, Is.EqualTo(DeclineReason.AccountClosed));
            Assert.That(_account.Balance, Is.EqualTo(100m));
        });
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AnotherCurrency_IsDeclined()
    {
        var declined = await Post(PostingOperation.Record, -50m, currency: "EUR");

        Assert.That(declined, Is.EqualTo(DeclineReason.CurrencyMismatch));
    }

    [Test]
    public async Task MessageHandler_RepliesPosted_OrDeclinedWithTheReason()
    {
        var sender = new Mock<ISender>();
        sender.SetupSequence(x => x.Send(It.IsAny<PostTransactionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeclineReason?)null)
            .ReturnsAsync(DeclineReason.AccountClosed);
        var bus = new FakeBus();
        var handler = new PostTransactionMessageHandler(sender.Object, bus);
        var step = Guid.NewGuid();
        var message = new PostTransaction(step, Transaction, _account.Id.Value, Owner, PostingOperation.Record, -50m, "PLN", Day);

        await handler.Handle(message);
        await handler.Handle(message);

        Assert.That(bus.Events.OfType<ReplyMessageSent>().Select(x => x.ReplyMessage), Is.EqualTo(new object[]
        {
            new TransactionPosted(step, Transaction),
            new TransactionDeclined(step, Transaction, DeclineReason.AccountClosed)
        }));
    }

    [Test]
    public async Task BalanceHistory_RunsFromTheOpeningBalance_AndKeepsTheAskedDays()
    {
        var readService = new Mock<IBalanceHistoryReadService>();
        readService.Setup(x => x.GetMovementsAsync(_account.Id, new OwnerId(Owner), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BalanceMovements(100m,
            [
                new DailyTotal(Day, -50m),
                new DailyTotal(Day.AddDays(1), 20m),
                new DailyTotal(Day.AddDays(2), -5m)
            ]));
        var handler = new GetBalanceHistoryHandler(readService.Object);

        var history = await handler.Handle(
            new GetBalanceHistoryQuery(Owner, _account.Id.Value, From: Day.AddDays(1), To: null), CancellationToken.None);

        Assert.That(history, Is.EqualTo(new[]
        {
            new DailyBalanceDto(Day.AddDays(1), 70m),
            new DailyBalanceDto(Day.AddDays(2), 65m)
        }));
    }

    [Test]
    public void BalanceHistory_OfAnUnknownAccount_IsNotFound()
    {
        var handler = new GetBalanceHistoryHandler(Mock.Of<IBalanceHistoryReadService>());

        Assert.ThrowsAsync<AccountNotFoundException>(() =>
            handler.Handle(new GetBalanceHistoryQuery(Owner, Guid.NewGuid(), null, null), CancellationToken.None));
    }

    private Task<DeclineReason?> Post(PostingOperation operation, decimal amount, Guid? step = null, string currency = "PLN")
        => _handler.Handle(
            new PostTransactionCommand(step ?? Guid.NewGuid(), Transaction, _account.Id.Value, Owner, operation, amount, currency, Day),
            CancellationToken.None);
}

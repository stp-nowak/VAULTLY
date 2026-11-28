using MediatR;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NUnit.Framework;

using Rebus.TestHelpers;
using Rebus.TestHelpers.Events;

using Vaultly.Accounts.PublicTypes.Commands;
using Vaultly.Transactions.Application.Features.Postings;
using Vaultly.Transactions.Application.Features.Postings.ConfirmPosting;
using Vaultly.Transactions.Application.Features.Postings.DeclinePosting;
using Vaultly.Transactions.Domain.Transactions;
using Vaultly.Transactions.Infrastructure.Postings;

using AccountsDeclineReason = Vaultly.Accounts.PublicTypes.Commands.DeclineReason;
using DeclineReason = Vaultly.Transactions.Domain.Transactions.DeclineReason;

namespace Vaultly.Transactions.Application.Tests;

[TestFixture]
public sealed class TransactionPostingSagaTests
{
    private static readonly Guid Step = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Transaction = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Account = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateOnly Day = new(2026, 11, 20);

    private FakeBus _bus = null!;
    private Mock<ISender> _sender = null!;
    private SagaFixture<TransactionPostingSaga> _saga = null!;

    [SetUp]
    public void SetUp()
    {
        _bus = new FakeBus();
        _sender = new Mock<ISender>();
        _saga = SagaFixture.For(() => new TransactionPostingSaga(_bus, _sender.Object, NullLogger<TransactionPostingSaga>.Instance));
    }

    [TearDown]
    public void TearDown() => _saga.Dispose();

    [Test]
    public void Start_SendsTheStepToAccounts_AndWaitsForAnAnswer()
    {
        _saga.Deliver(new StartPosting(Step, Transaction, Account, Owner, PostingStep.Change, -80m, "PLN", Day));

        Assert.Multiple(() =>
        {
            Assert.That(SentToAccounts(), Is.EqualTo(new[]
            {
                new PostTransaction(Step, Transaction, Account, Owner, PostingOperation.Change, -80m, "PLN", Day)
            }));
            Assert.That(_bus.Events.OfType<MessageDeferredToSelf>().Select(x => x.CommandMessage),
                Is.EqualTo(new object[] { new PostingTimedOut(Step) }));
            Assert.That(_saga.Data.OfType<PostingSagaData>().Single().Attempts, Is.EqualTo(1));
        });
    }

    [Test]
    public void Posted_ConfirmsTheStep_AndEndsTheSaga()
    {
        Start();

        _saga.Deliver(new TransactionPosted(Step, Transaction));

        _sender.Verify(x => x.Send(new ConfirmPostingCommand(Transaction, Step), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(_saga.Data, Is.Empty);
    }

    [Test]
    public void Declined_UndoesTheStep_WithTheReason_AndEndsTheSaga()
    {
        Start();

        _saga.Deliver(new TransactionDeclined(Step, Transaction, AccountsDeclineReason.AccountClosed));

        _sender.Verify(
            x => x.Send(new DeclinePostingCommand(Transaction, Step, DeclineReason.AccountClosed), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.That(_saga.Data, Is.Empty);
    }

    [Test]
    public void WithoutAnAnswer_TheStepIsSentAgain_UpToTheLimit()
    {
        Start();

        for (var i = 0; i < TransactionPostingSaga.MaxAttempts + 2; i++)
        {
            _saga.Deliver(new PostingTimedOut(Step));
        }

        Assert.Multiple(() =>
        {
            Assert.That(SentToAccounts(), Has.Count.EqualTo(TransactionPostingSaga.MaxAttempts));
            Assert.That(_saga.Data, Is.Not.Empty);
        });
    }

    [Test]
    public void TheSameStartAgain_SendsNothingMore()
    {
        Start();

        Start();

        Assert.That(SentToAccounts(), Has.Count.EqualTo(1));
    }

    private void Start()
        => _saga.Deliver(new StartPosting(Step, Transaction, Account, Owner, PostingStep.Record, -50m, "PLN", Day));

    private List<object> SentToAccounts() => [.. _bus.Events.OfType<MessageSent>().Select(x => x.CommandMessage)];
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Vaultly.Transactions.Application.Features.Accounts.TrackAccountClosed;
using Vaultly.Transactions.Application.Features.Accounts.TrackAccountOpened;
using Vaultly.Transactions.Application.Features.Postings;
using Vaultly.Transactions.Application.Features.Postings.ConfirmPosting;
using Vaultly.Transactions.Application.Features.Postings.DeclinePosting;
using Vaultly.Transactions.Domain.Transactions;

namespace Vaultly.Transactions.Application.Tests;

[TestFixture]
public sealed class TransactionsApiTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AccountId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private TransactionsApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        _factory = new TransactionsApiFactory();
        _client = _factory.ClientFor(UserId);
        await SendAsync(new TrackAccountOpenedCommand(AccountId, UserId, "PLN", 1));
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Transactions_WithoutAGatewayToken_AreUnauthorized()
    {
        using var anonymous = _factory.CreateClient();

        Assert.That((await anonymous.GetAsync("/transactions")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task HealthEndpoints_AnswerWithoutAToken()
    {
        using var anonymous = _factory.CreateClient();

        Assert.Multiple(async () =>
        {
            Assert.That((await anonymous.GetAsync("/health/live")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That((await anonymous.GetAsync("/health/ready")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task Record_IsPending_AndStartsThePostingSaga()
    {
        var response = await RecordAsync(-50.25m);
        var transaction = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers.Location?.ToString(), Is.EqualTo($"/transactions/{transaction.GetProperty("id").GetGuid()}"));
            Assert.That(transaction.GetProperty("status").GetString(), Is.EqualTo("pending"));
            Assert.That(transaction.GetProperty("pendingStep").GetString(), Is.EqualTo("record"));
            Assert.That(transaction.GetProperty("currency").GetString(), Is.EqualTo("PLN"));
            Assert.That(transaction.GetProperty("bookedOn").GetString(), Is.EqualTo("2026-11-20"));
            Assert.That(_factory.Bus.OfType<StartPosting>().Single().Amount, Is.EqualTo(-50.25m));
        });
    }

    [Test]
    public async Task PostedTransaction_ChangesAndDeletes_ThroughAccounts()
    {
        var id = await RecordIdAsync(-50m);
        await ConfirmLastStepAsync();

        var describe = await _client.PutAsJsonAsync($"/transactions/{id}", new { amount = -50m, bookedOn = "2026-11-20", description = "Groceries" });
        var change = await _client.PutAsJsonAsync($"/transactions/{id}", new { amount = -80m, bookedOn = "2026-11-20", description = "Groceries" });
        var whilePending = await _client.DeleteAsync($"/transactions/{id}");
        await ConfirmLastStepAsync();
        var delete = await _client.DeleteAsync($"/transactions/{id}");
        await ConfirmLastStepAsync();

        Assert.Multiple(async () =>
        {
            Assert.That(describe.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(change.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That(whilePending.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            Assert.That((await _client.GetAsync($"/transactions/{id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task DeclinedTransaction_IsRejected_AndCanOnlyBeDeleted()
    {
        var id = await RecordIdAsync(-50m);
        var step = _factory.Bus.OfType<StartPosting>().Last();
        await SendAsync(new DeclinePostingCommand(id, step.StepId, DeclineReason.AccountClosed));

        var transaction = await _client.GetFromJsonAsync<JsonElement>($"/transactions/{id}");
        var change = await _client.PutAsJsonAsync($"/transactions/{id}", new { amount = -80m, bookedOn = "2026-11-20", description = "Shop" });
        var delete = await _client.DeleteAsync($"/transactions/{id}");

        Assert.Multiple(() =>
        {
            Assert.That(transaction.GetProperty("status").GetString(), Is.EqualTo("rejected"));
            Assert.That(transaction.GetProperty("declineReason").GetString(), Is.EqualTo("account_closed"));
            Assert.That(change.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        });
    }

    [Test]
    public async Task List_FiltersByAccount_AndShowsOnlyTheUsersTransactions()
    {
        var id = await RecordIdAsync(-50m);
        using var otherUser = _factory.ClientFor(OtherUserId);

        Assert.Multiple(async () =>
        {
            var mine = await _client.GetFromJsonAsync<JsonElement>($"/transactions?accountId={AccountId}");
            Assert.That(mine.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()), Is.EqualTo(new[] { id }));
            Assert.That((await _client.GetFromJsonAsync<JsonElement>($"/transactions?accountId={Guid.NewGuid()}")).GetArrayLength(), Is.Zero);
            Assert.That((await otherUser.GetFromJsonAsync<JsonElement>("/transactions")).GetArrayLength(), Is.Zero);
            Assert.That((await otherUser.GetAsync($"/transactions/{id}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task Record_OnAClosedAccount_IsAConflict_AndOnAnUnknownOne_NotFound()
    {
        await SendAsync(new TrackAccountClosedCommand(AccountId, UserId, 2));

        var closed = await RecordAsync(-50m);
        var unknown = await _client.PostAsJsonAsync("/transactions",
            new { accountId = Guid.NewGuid(), amount = -50m, bookedOn = "2026-11-20", description = "Shop" });

        Assert.Multiple(() =>
        {
            Assert.That(closed.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    private static IEnumerable<TestCaseData> InvalidTransactions()
    {
        yield return new TestCaseData(new { accountId = AccountId, amount = -50m, bookedOn = "2026-11-20", description = " " }).SetName("blank description");
        yield return new TestCaseData(new { accountId = AccountId, amount = 0.00001m, bookedOn = "2026-11-20", description = "Shop" }).SetName("too precise amount");
    }

    [TestCaseSource(nameof(InvalidTransactions))]
    public async Task Record_WithInvalidInput_IsABadRequest(object request)
    {
        var response = await _client.PostAsJsonAsync("/transactions", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private Task<HttpResponseMessage> RecordAsync(decimal amount)
        => _client.PostAsJsonAsync("/transactions",
            new { accountId = AccountId, amount, bookedOn = "2026-11-20", description = "Shop", note = "weekly" });

    private async Task<Guid> RecordIdAsync(decimal amount)
    {
        var response = await RecordAsync(amount);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private Task ConfirmLastStepAsync()
    {
        var step = _factory.Bus.OfType<StartPosting>().Last();
        return SendAsync(new ConfirmPostingCommand(step.TransactionId, step.StepId));
    }

    private async Task SendAsync(IBaseRequest command)
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send((object)command);
    }
}

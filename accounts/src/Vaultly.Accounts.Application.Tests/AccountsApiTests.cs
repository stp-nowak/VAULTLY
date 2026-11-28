using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using NUnit.Framework;

using Vaultly.Accounts.PublicTypes.Events;

namespace Vaultly.Accounts.Application.Tests;

[TestFixture]
public sealed class AccountsApiTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private AccountsApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new AccountsApiFactory();
        _client = _factory.ClientFor(UserId);
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Accounts_WithoutAGatewayToken_AreUnauthorized()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/accounts");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
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
    public async Task OpenAccount_ReturnsTheAccountWithItsLocation_InWireSpelling()
    {
        var response = await OpenAsync("Visa", "credit_card", "pln", -120.5m);
        var account = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers.Location?.ToString(), Is.EqualTo($"/accounts/{account.GetProperty("id").GetGuid()}"));
            Assert.That(account.GetProperty("name").GetString(), Is.EqualTo("Visa"));
            Assert.That(account.GetProperty("type").GetString(), Is.EqualTo("credit_card"));
            Assert.That(account.GetProperty("currency").GetString(), Is.EqualTo("PLN"));
            Assert.That(account.GetProperty("balance").GetDecimal(), Is.EqualTo(-120.5m));
            Assert.That(account.GetProperty("closedAt").ValueKind, Is.EqualTo(JsonValueKind.Null));
        });
    }

    [Test]
    public async Task Accounts_ListRenameCloseAndNetWorth_WorkTogether()
    {
        var wallet = await OpenIdAsync("Wallet", "cash", "PLN", 300m);
        var card = await OpenIdAsync("Card", "credit_card", "PLN", -100m);
        var euro = await OpenIdAsync("Euro", "savings", "EUR", 50m);

        var renamed = await _client.PutAsJsonAsync($"/accounts/{wallet}/name", new { name = "Cash" });
        var closed = await _client.PostAsync($"/accounts/{euro}/close", content: null);
        var accounts = await _client.GetFromJsonAsync<JsonElement>("/accounts");
        var allAccounts = await _client.GetFromJsonAsync<JsonElement>("/accounts?includeClosed=true");
        var netWorth = await _client.GetFromJsonAsync<JsonElement>("/accounts/net-worth");

        Assert.Multiple(() =>
        {
            Assert.That(renamed.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(closed.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(accounts.EnumerateArray().Select(x => x.GetProperty("name").GetString()), Is.EqualTo(new[] { "Cash", "Card" }));
            Assert.That(allAccounts.GetArrayLength(), Is.EqualTo(3));
            Assert.That(netWorth.GetArrayLength(), Is.EqualTo(1));
            Assert.That(netWorth[0].GetProperty("currency").GetString(), Is.EqualTo("PLN"));
            Assert.That(netWorth[0].GetProperty("assets").GetDecimal(), Is.EqualTo(300m));
            Assert.That(netWorth[0].GetProperty("liabilities").GetDecimal(), Is.EqualTo(100m));
            Assert.That(netWorth[0].GetProperty("netWorth").GetDecimal(), Is.EqualTo(200m));
        });
        Assert.That(card, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task ClosedAccount_CannotBeClosedAgain()
    {
        var account = await OpenIdAsync("Wallet", "cash", "PLN", 0m);
        await _client.PostAsync($"/accounts/{account}/close", content: null);

        var response = await _client.PostAsync($"/accounts/{account}/close", content: null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        });
    }

    [Test]
    public async Task OpeningAndClosing_TellOtherServices()
    {
        var account = await OpenIdAsync("Wallet", "cash", "pln", 0m);
        await _client.PostAsync($"/accounts/{account}/close", content: null);

        Assert.That(_factory.Bus.Messages, Is.EqualTo(new object[]
        {
            new AccountOpened(account, UserId, "PLN", Version: 1),
            new AccountClosed(account, UserId, Version: 2)
        }));
    }

    [Test]
    public async Task BalanceHistory_OfANewAccount_IsEmpty_AndOfAnotherUsersAccount_NotFound()
    {
        var account = await OpenIdAsync("Wallet", "cash", "PLN", 0m);
        using var otherUser = _factory.ClientFor(OtherUserId);

        Assert.Multiple(async () =>
        {
            Assert.That((await _client.GetFromJsonAsync<JsonElement>($"/accounts/{account}/balances?from=2026-01-01")).GetArrayLength(), Is.Zero);
            Assert.That((await otherUser.GetAsync($"/accounts/{account}/balances")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task AnotherUsersAccount_IsNotFound()
    {
        var account = await OpenIdAsync("Wallet", "cash", "PLN", 0m);
        using var otherUser = _factory.ClientFor(OtherUserId);

        Assert.Multiple(async () =>
        {
            Assert.That((await otherUser.GetAsync($"/accounts/{account}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That((await otherUser.PostAsync($"/accounts/{account}/close", content: null)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That((await otherUser.GetFromJsonAsync<JsonElement>("/accounts")).GetArrayLength(), Is.Zero);
        });
    }

    private static IEnumerable<TestCaseData> InvalidAccounts()
    {
        yield return new TestCaseData(new { name = "Wallet", type = "cash", currency = "XYZ", openingBalance = 0 }).SetName("unknown currency");
        yield return new TestCaseData(new { name = " ", type = "cash", currency = "PLN", openingBalance = 0 }).SetName("blank name");
        yield return new TestCaseData(new { name = "Wallet", currency = "PLN", openingBalance = 0 }).SetName("missing type");
        yield return new TestCaseData(new { name = "Wallet", type = "0", currency = "PLN", openingBalance = 0 }).SetName("type as a number string");
        yield return new TestCaseData(new { name = "Wallet", type = 0, currency = "PLN", openingBalance = 0 }).SetName("type as a number");
        yield return new TestCaseData(new { name = "Wallet", type = "cash", currency = "PLN", openingBalance = 0.00001m }).SetName("too precise amount");
    }

    [TestCaseSource(nameof(InvalidAccounts))]
    public async Task OpenAccount_WithInvalidInput_IsABadRequest(object request)
    {
        var response = await _client.PostAsJsonAsync("/accounts", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private Task<HttpResponseMessage> OpenAsync(string name, string type, string currency, decimal openingBalance)
        => _client.PostAsJsonAsync("/accounts", new { name, type, currency, openingBalance });

    private async Task<Guid> OpenIdAsync(string name, string type, string currency, decimal openingBalance)
    {
        var response = await OpenAsync(name, type, currency, openingBalance);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
}

using NUnit.Framework;

using Vaultly.Identity.Domain.Aggregates;
using Vaultly.Identity.Domain.Constants;
using Vaultly.Identity.Domain.Events;
using Vaultly.Identity.Domain.Exceptions;
using Vaultly.Identity.Domain.ValueObjects;

namespace Vaultly.Identity.Domain.Tests;

[TestFixture]
public sealed class AuthorizationTests
{
    // RFC 7636 Appendix B test vector.
    private const string Verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
    private const string Challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

    private static readonly DateTimeOffset Now = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly ClientId Client = new("vaultly-web");
    private static readonly RedirectUri Callback = new("https://app.test/api/auth/callback");

    [Test]
    public void CodeChallenge_IsSatisfiedByMatchingVerifierOnly()
    {
        var challenge = new CodeChallenge(Challenge);

        Assert.Multiple(() =>
        {
            Assert.That(challenge.IsSatisfiedBy(Verifier), Is.True);
            Assert.That(challenge.IsSatisfiedBy(Verifier[..^1] + "Y"), Is.False);
            Assert.That(challenge.IsSatisfiedBy("too-short"), Is.False);
            Assert.That(challenge.IsSatisfiedBy(null), Is.False);
        });
    }

    [TestCase("")]
    [TestCase("plain-verifier-is-not-a-challenge")]
    [TestCase("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw+cM")]
    public void CodeChallenge_RejectsValuesThatAreNotS256Hashes(string value)
        => Assert.That(() => new CodeChallenge(value), Throws.TypeOf<InvalidValueException>());

    [Test]
    public void AuthorizationRequest_Complete_RaisesEventAndCannotBeCompletedTwice()
    {
        var request = CreateRequest();
        request.ClearDomainEvents();

        request.Complete(Now.AddMinutes(1));

        Assert.Multiple(() =>
        {
            Assert.That(request.IsCompleted, Is.True);
            Assert.That(request.DomainEvents.OfType<AuthorizationRequestCompletedDomainEvent>().Single().AuthorizationRequestId, Is.EqualTo(request.Id));
            Assert.That(() => request.Complete(Now.AddMinutes(2)), Throws.TypeOf<AuthorizationRequestCompletedException>());
        });
    }

    [Test]
    public void AuthorizationRequest_CreateForConsole_RemembersConsoleUrlWithoutClientBinding()
    {
        var consoleUrl = new RedirectUri("https://identity.test/admin");

        var request = AuthorizationRequest.CreateForConsole(consoleUrl, Now, Now.AddMinutes(10));

        Assert.Multiple(() =>
        {
            Assert.That(request.IsForConsole, Is.True);
            Assert.That(request.ClientId, Is.EqualTo(ConsoleClient.Id));
            Assert.That(request.RedirectUri, Is.EqualTo(consoleUrl));
            Assert.That(request.State, Is.Null);
            Assert.That(request.CodeChallenge, Is.Null);
        });
    }

    [Test]
    public void AuthorizationRequest_Create_ForClient_IsNotForConsole()
        => Assert.That(CreateRequest().IsForConsole, Is.False);

    [Test]
    public void AuthorizationRequest_Create_WithConsoleClientId_Throws()
        => Assert.That(
            () => AuthorizationRequest.Create(ConsoleClient.Id, Callback, new ClientState("s"), new CodeChallenge(Challenge), Now, Now.AddMinutes(10)),
            Throws.TypeOf<InvalidValueException>());

    [Test]
    public void AuthorizationRequest_EnsurePending_WhenExpired_Throws()
    {
        var request = CreateRequest();

        Assert.That(() => request.EnsurePending(Now.AddMinutes(11)), Throws.TypeOf<AuthorizationRequestExpiredException>());
    }

    [Test]
    public void ConsumeAuthCode_WhenClientRedirectAndVerifierMatch_ConsumesCode()
    {
        var session = CreateSessionWithCode();

        var code = session.ConsumeAuthCode(new CodeHash("code-hash"), Client, Callback, Verifier, Now.AddMinutes(1));

        Assert.That(code.IsUsed, Is.True);
    }

    [TestCase("other-client", "https://app.test/api/auth/callback", Verifier, "client")]
    [TestCase("vaultly-web", "https://evil.test/callback", Verifier, "redirect_uri")]
    [TestCase("vaultly-web", "https://app.test/api/auth/callback", "wrong-verifier-wrong-verifier-wrong-verifier-1", "code_verifier")]
    public void ConsumeAuthCode_WhenBindingDoesNotMatch_ThrowsAndBurnsCode(string clientId, string redirectUri, string verifier, string reason)
    {
        var session = CreateSessionWithCode();
        var codeHash = new CodeHash("code-hash");

        var ex = Assert.Throws<AuthCodeMismatchException>(
            () => session.ConsumeAuthCode(codeHash, new ClientId(clientId), new RedirectUri(redirectUri), verifier, Now.AddMinutes(1)));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Reason, Is.EqualTo(reason));
            Assert.That(
                () => session.ConsumeAuthCode(codeHash, Client, Callback, Verifier, Now.AddMinutes(1)),
                Throws.TypeOf<AuthCodeAlreadyUsedException>(),
                "a failed redemption must burn the code");
        });
    }

    [Test]
    public void IsSsoActive_IsFalseAfterExpiryOrRevocation()
    {
        var session = CreateSessionWithCode();

        Assert.Multiple(() =>
        {
            Assert.That(session.IsSsoActive(Now.AddHours(1)), Is.True);
            Assert.That(session.IsSsoActive(Now.AddHours(13)), Is.False);
        });

        session.RevokeWithTokens(Now.AddHours(2));
        Assert.That(session.IsSsoActive(Now.AddHours(3)), Is.False);
    }

    private static AuthorizationRequest CreateRequest()
        => AuthorizationRequest.Create(Client, Callback, new ClientState("client-state"), new CodeChallenge(Challenge), Now, Now.AddMinutes(10));

    private static Session CreateSessionWithCode()
    {
        var session = Session.Create(UserId.New(), new AuthMethod("google"), null, null, new TokenHash("sso-token"), Now, Now.AddHours(12));
        session.IssueAuthCode(new CodeHash("code-hash"), Now.AddMinutes(2), Client, Callback, new CodeChallenge(Challenge));
        return session;
    }
}

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using NUnit.Framework;

using Vaultly.Identity.Api.Contracts;
using Vaultly.Identity.Api.Endpoints;
using Vaultly.Identity.Api.Http;
using Vaultly.Identity.Application.Common;
using Vaultly.Identity.Application.Features.Passwords.RegisterWithPassword;
using Vaultly.Identity.Application.Features.Passwords.SignInWithPassword;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Domain;
using Vaultly.Identity.Domain.AuthorizationRequests;
using Vaultly.Identity.Domain.Users;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class PasswordEndpointsTests
{
    private static readonly AuthCallbackResult SignedIn = new("https://app.test/callback?code=c&state=s", "sso-token", DateTimeOffset.UtcNow.AddHours(1));

    private Mock<ISender> _sender = null!;

    [SetUp]
    public void SetUp() => _sender = new Mock<ISender>();

    [Test]
    public async Task SignIn_OnSuccess_SetsTheSsoCookieAndReturnsWhereToGo()
    {
        _sender.Setup(x => x.Send(It.IsAny<SignInWithPasswordCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(SignedIn);
        var httpContext = CreateHttpContext();

        var result = await PasswordEndpoints.SignInAsync(Guid.NewGuid(), new PasswordEndpoints.PasswordSignInRequest("ada@example.com", "pw"), httpContext, _sender.Object);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<IStatusCodeHttpResult>().And.Property(nameof(IStatusCodeHttpResult.StatusCode)).EqualTo(200));
            Assert.That(httpContext.Response.Headers.SetCookie.ToString(), Does.StartWith($"{SsoCookie.Name}=sso-token"));
        });
    }

    [Test]
    public async Task SignIn_PassesTheClientDetailsAndMissingFieldsAsEmpty()
    {
        var id = Guid.NewGuid();
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.UserAgent = "browser";
        _sender.Setup(x => x.Send(It.IsAny<SignInWithPasswordCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(SignedIn);

        await PasswordEndpoints.SignInAsync(id, new PasswordEndpoints.PasswordSignInRequest(null, null), httpContext, _sender.Object);

        _sender.Verify(x => x.Send(new SignInWithPasswordCommand(id, "", "", "browser", null), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IEnumerable<TestCaseData> Failures()
    {
        yield return new TestCaseData(PasswordSignInException.WrongCredentials(), 400, ApiError.InvalidCredentials);
        yield return new TestCaseData(PasswordSignInException.EmailAlreadyUsed(), 409, ApiError.EmailTaken);
        yield return new TestCaseData(new WeakPasswordException(), 400, ApiError.WeakPassword);
        yield return new TestCaseData(new AuthorizationRequestExpiredException(), 400, ApiError.RequestExpired);
        yield return new TestCaseData(new InvalidValueException("Email is invalid."), 400, ApiError.InvalidInput);
        yield return new TestCaseData(new InvalidOperationException("Sign-in method is disabled."), 403, ApiError.SignInMethodDisabled);
    }

    [TestCaseSource(nameof(Failures))]
    public async Task Register_MapsFailuresToCodesTheLoginUiUnderstands(Exception failure, int status, ApiError error)
    {
        _sender.Setup(x => x.Send(It.IsAny<RegisterWithPasswordCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var httpContext = CreateHttpContext();

        var result = await PasswordEndpoints.RegisterAsync(
            Guid.NewGuid(), new PasswordEndpoints.RegistrationRequest("Ada", "ada@example.com", "pw"), httpContext, _sender.Object);

        Assert.Multiple(() =>
        {
            Assert.That(((IStatusCodeHttpResult)result).StatusCode, Is.EqualTo(status));
            Assert.That(((IValueHttpResult)result).Value, Is.EqualTo(new ApiErrorResponse(error)));
            Assert.That(httpContext.Response.Headers.SetCookie.ToString(), Is.Empty);
        });
    }

    private DefaultHttpContext CreateHttpContext()
        => new() { RequestServices = new ServiceCollection().AddSingleton(_sender.Object).BuildServiceProvider() };
}

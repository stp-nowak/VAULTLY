using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using NUnit.Framework;

using Vaultly.Identity.Api.Endpoints;
using Vaultly.Identity.Api.Http;
using Vaultly.Identity.Application.Commands.AssignUserRole;
using Vaultly.Identity.Application.Commands.SetSignInMethodEnabled;
using Vaultly.Identity.Application.Models;
using Vaultly.Identity.Application.Queries.GetConsoleUser;
using Vaultly.Identity.Domain.Enums;
using Vaultly.Identity.Domain.Exceptions;

namespace Vaultly.Identity.Application.Tests;

[TestFixture]
public sealed class AdminEndpointsTests
{
    private static readonly ConsoleUserDto Admin = new(Guid.NewGuid(), "admin@example.com", "Admin", UserRole.Admin);

    private Mock<ISender> _sender = null!;
    private bool _nextCalled;

    [SetUp]
    public void SetUp()
    {
        _sender = new Mock<ISender>();
        _nextCalled = false;
    }

    [Test]
    public async Task RequireCsrfHeader_WithoutHeader_RejectsAndMarksResponseNoStore()
    {
        var httpContext = CreateHttpContext();

        var result = await CsrfHeaderFilter.Require(Invocation(httpContext), Next);

        Assert.Multiple(() =>
        {
            Assert.That(StatusOf(result), Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(_nextCalled, Is.False);
            Assert.That(httpContext.Response.Headers.CacheControl.ToString(), Is.EqualTo("no-store"));
        });
    }

    [Test]
    public async Task RequireCsrfHeader_WithHeader_Continues()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[CsrfHeaderFilter.HeaderName] = "1";

        await CsrfHeaderFilter.Require(Invocation(httpContext), Next);

        Assert.That(_nextCalled, Is.True);
    }

    [Test]
    public async Task RequireAdmin_WithoutSession_ReturnsUnauthorized()
    {
        _sender.Setup(x => x.Send(It.IsAny<GetConsoleUserQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync((ConsoleUserDto?)null);

        var result = await AdminEndpoints.RequireAdminAsync(Invocation(CreateHttpContext()), Next);

        Assert.Multiple(() =>
        {
            Assert.That(StatusOf(result), Is.EqualTo(StatusCodes.Status401Unauthorized));
            Assert.That(_nextCalled, Is.False);
        });
    }

    [Test]
    public async Task RequireAdmin_ForRegularUser_ReturnsForbidden()
    {
        _sender.Setup(x => x.Send(It.IsAny<GetConsoleUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Admin with { Role = UserRole.User });

        var result = await AdminEndpoints.RequireAdminAsync(Invocation(CreateHttpContext()), Next);

        Assert.Multiple(() =>
        {
            Assert.That(StatusOf(result), Is.EqualTo(StatusCodes.Status403Forbidden));
            Assert.That(_nextCalled, Is.False);
        });
    }

    [Test]
    public async Task RequireAdmin_ForAdmin_PassesTheSsoTokenAndContinues()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Cookie = $"{SsoCookie.Name}=sso-token";
        _sender.Setup(x => x.Send(new GetConsoleUserQuery("sso-token"), It.IsAny<CancellationToken>())).ReturnsAsync(Admin);

        await AdminEndpoints.RequireAdminAsync(Invocation(httpContext), Next);

        Assert.Multiple(() =>
        {
            Assert.That(_nextCalled, Is.True);
            Assert.That(AdminEndpoints.GetCurrentAdmin(httpContext), Is.InstanceOf<Ok<ConsoleUserDto>>());
        });
    }

    [TestCase(true, StatusCodes.Status204NoContent)]
    [TestCase(false, StatusCodes.Status404NotFound)]
    public async Task AssignRole_MapsTheOutcome(bool assigned, int expectedStatus)
    {
        var httpContext = await SignedInAdminContextAsync();
        _sender.Setup(x => x.Send(It.IsAny<AssignUserRoleCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(assigned);

        var result = await AdminEndpoints.AssignRoleAsync(Guid.NewGuid(), new AdminEndpoints.AssignRoleRequest(UserRole.Admin), httpContext, _sender.Object);

        Assert.That(StatusOf(result), Is.EqualTo(expectedStatus));
    }

    [Test]
    public async Task AssignRole_RecordsTheAdminMakingTheChange()
    {
        var httpContext = await SignedInAdminContextAsync();
        var userId = Guid.NewGuid();

        await AdminEndpoints.AssignRoleAsync(userId, new AdminEndpoints.AssignRoleRequest(UserRole.Admin), httpContext, _sender.Object);

        _sender.Verify(x => x.Send(new AssignUserRoleCommand(Admin.Id, userId, UserRole.Admin), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AssignRole_WhenLastAdmin_ReturnsConflict()
    {
        var httpContext = await SignedInAdminContextAsync();
        _sender.Setup(x => x.Send(It.IsAny<AssignUserRoleCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(new LastAdminException());

        var result = await AdminEndpoints.AssignRoleAsync(Guid.NewGuid(), new AdminEndpoints.AssignRoleRequest(UserRole.User), httpContext, _sender.Object);

        Assert.That(StatusOf(result), Is.EqualTo(StatusCodes.Status409Conflict));
    }

    [TestCase(true, StatusCodes.Status204NoContent)]
    [TestCase(false, StatusCodes.Status404NotFound)]
    public async Task SetSignInMethodEnabled_MapsTheOutcomeAndRecordsTheAdmin(bool changed, int expectedStatus)
    {
        var httpContext = await SignedInAdminContextAsync();
        _sender.Setup(x => x.Send(It.IsAny<SetSignInMethodEnabledCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(changed);

        var result = await AdminEndpoints.SetSignInMethodEnabledAsync("google", new AdminEndpoints.SetSignInMethodRequest(false), httpContext, _sender.Object);

        Assert.That(StatusOf(result), Is.EqualTo(expectedStatus));
        _sender.Verify(x => x.Send(new SetSignInMethodEnabledCommand(Admin.Id, "google", false), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetSignInMethodEnabled_WhenLastMethod_ReturnsConflict()
    {
        var httpContext = await SignedInAdminContextAsync();
        _sender.Setup(x => x.Send(It.IsAny<SetSignInMethodEnabledCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new LastSignInMethodException());

        var result = await AdminEndpoints.SetSignInMethodEnabledAsync("google", new AdminEndpoints.SetSignInMethodRequest(false), httpContext, _sender.Object);

        Assert.That(StatusOf(result), Is.EqualTo(StatusCodes.Status409Conflict));
    }

    private async Task<HttpContext> SignedInAdminContextAsync()
    {
        var httpContext = CreateHttpContext();
        _sender.Setup(x => x.Send(It.IsAny<GetConsoleUserQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(Admin);
        await AdminEndpoints.RequireAdminAsync(Invocation(httpContext), Next);
        _nextCalled = false;
        return httpContext;
    }

    private DefaultHttpContext CreateHttpContext()
        => new()
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .AddSingleton(_sender.Object)
                .BuildServiceProvider(),
        };

    private static DefaultEndpointFilterInvocationContext Invocation(HttpContext httpContext) => new(httpContext);

    private ValueTask<object?> Next(EndpointFilterInvocationContext context)
    {
        _nextCalled = true;
        return ValueTask.FromResult<object?>(Results.Ok());
    }

    private static int? StatusOf(object? result) => (result as IStatusCodeHttpResult)?.StatusCode;
}

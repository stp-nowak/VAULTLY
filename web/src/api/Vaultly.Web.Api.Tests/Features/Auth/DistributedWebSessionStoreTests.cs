using System.Text;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NUnit.Framework;

using Vaultly.Web.Api.Features.Auth.Sessions;

namespace Vaultly.Web.Api.Tests.Features.Auth;

[TestFixture]
public sealed class DistributedWebSessionStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task SetAsync_StoresEncryptedPayloadUnderHashedKeyUntilRefreshTokenExpires()
    {
        var cache = new Mock<IDistributedCache>();
        string? storedKey = null;
        byte[]? storedValue = null;
        DistributedCacheEntryOptions? storedOptions = null;
        cache.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>((key, value, options, _) =>
            {
                storedKey = key;
                storedValue = value;
                storedOptions = options;
            })
            .Returns(Task.CompletedTask);
        var session = WebSession.Create(TestData.Tokens(Now), Now);
        var sut = new DistributedWebSessionStore(cache.Object, new EphemeralDataProtectionProvider(), NullLogger<DistributedWebSessionStore>.Instance);

        await sut.SetAsync("raw-session-id", session, CancellationToken.None);

        var payloadText = Encoding.UTF8.GetString(storedValue!);
        Assert.Multiple(() =>
        {
            Assert.That(storedKey, Does.Not.Contain("raw-session-id"));
            Assert.That(payloadText, Does.Not.Contain("access-1").And.Not.Contain("refresh-1"));
            Assert.That(storedOptions!.AbsoluteExpiration, Is.EqualTo(session.RefreshTokenExpiresAt));
        });
    }

    [Test]
    public async Task GetAsync_WhenPayloadCannotBeDecrypted_ReturnsNullAndRemovesEntry()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Encoding.UTF8.GetBytes("garbage"));
        cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = new DistributedWebSessionStore(cache.Object, new EphemeralDataProtectionProvider(), NullLogger<DistributedWebSessionStore>.Instance);

        var session = await sut.GetAsync("raw-session-id", CancellationToken.None);

        Assert.That(session, Is.Null);
        cache.Verify(x => x.RemoveAsync(DistributedWebSessionStore.BuildKey("raw-session-id"), It.IsAny<CancellationToken>()), Times.Once);
    }
}

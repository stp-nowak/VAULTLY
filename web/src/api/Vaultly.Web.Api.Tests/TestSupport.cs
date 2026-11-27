using System.Net;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

using Moq;

using Vaultly.Web.Api.Common.Hosting;
using Vaultly.Web.Api.Features.Auth.IdentityService;
using Vaultly.Web.Api.Features.Web.ApiProxy;

namespace Vaultly.Web.Api.Tests;

/// <summary>
/// Hosts the real web API in memory with Redis replaced by an in-memory cache and the Identity service mocked.
/// </summary>
internal sealed class WebApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Mock of the Identity service client used by every request.</summary>
    public Mock<IIdentityServiceClient> Identity { get; } = new(MockBehavior.Strict);

    /// <summary>Captures requests forwarded to the gateway.</summary>
    public RecordingHandler Gateway { get; } = new(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true}") });

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(PlatformServiceCollectionExtensions.TestingEnvironment);
        builder.UseSetting("ConnectionStrings:Redis", string.Empty);
        builder.UseSetting("Web:GatewayBaseUrl", "https://gateway.test");
        builder.UseSetting("Auth:ClientSecret", "test-secret");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDistributedCache>();
            services.AddSingleton<IDistributedCache>(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));
            services.RemoveAll<IDataProtectionProvider>();
            services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            services.RemoveAll<IIdentityServiceClient>();
            services.AddSingleton(Identity.Object);
            services.RemoveAll<GatewayHttpClient>();
            services.AddSingleton(new GatewayHttpClient(new HttpMessageInvoker(Gateway)));
        });
    }

    /// <summary>
    /// Client that neither follows redirects nor manages cookies, so tests can assert on raw responses.
    /// </summary>
    public HttpClient CreateRawClient()
        => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
}

/// <summary>
/// Test helpers for tokens, cookies and time.
/// </summary>
internal static class TestData
{
    public static readonly AuthenticatedUser User = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "user@vaultly.test", "Vaultly User");

    /// <summary>Creates a token set with configurable access token lifetime.</summary>
    public static IdentityTokenSet Tokens(DateTimeOffset now, string accessToken = "access-1", string refreshToken = "refresh-1", TimeSpan? accessLifetime = null)
        => new(accessToken, now.Add(accessLifetime ?? TimeSpan.FromMinutes(15)), refreshToken, now.AddDays(30), Guid.Parse("22222222-2222-2222-2222-222222222222"), User);

    /// <summary>Reads all <c>Set-Cookie</c> headers of a response.</summary>
    public static IList<SetCookieHeaderValue> SetCookies(HttpResponseMessage response)
        => response.Headers.TryGetValues(HeaderNames.SetCookie, out var values)
            ? SetCookieHeaderValue.ParseList(values.ToList())
            : [];

    /// <summary>Builds a <c>Cookie</c> header value from non-deleted response cookies.</summary>
    public static string CookieHeader(params HttpResponseMessage[] responses)
        => string.Join("; ", responses
            .SelectMany(SetCookies)
            .Where(cookie => cookie.Expires is null || cookie.Expires > DateTimeOffset.UtcNow)
            .Select(cookie => $"{cookie.Name}={cookie.Value}"));
}

/// <summary>
/// Time provider frozen at a configurable instant.
/// </summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>
/// HTTP handler stub that records requests and answers with a configurable response.
/// </summary>
internal sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(respond(request));
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Vaultly.Web.Api.Features.Auth;
using Vaultly.Web.Api.Features.Auth.IdentityService;

namespace Vaultly.Web.Api.Tests.Features.Auth;

[TestFixture]
public sealed class AuthFeatureTests
{
    [Test]
    public void IdentityServiceClient_WhenNoInternalUrlConfigured_CallsTheBrowserFacingUrl()
    {
        var baseAddress = IdentityClientBaseAddress(internalBaseUrl: null);

        Assert.That(baseAddress, Is.EqualTo(new Uri("https://identity.test/")));
    }

    [Test]
    public void IdentityServiceClient_WhenInternalUrlConfigured_CallsItInsteadOfTheBrowserFacingUrl()
    {
        var baseAddress = IdentityClientBaseAddress(internalBaseUrl: "http://identity:8080");

        Assert.That(baseAddress, Is.EqualTo(new Uri("http://identity:8080/")));
    }

    private static Uri? IdentityClientBaseAddress(string? internalBaseUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:IdentityBaseUrl"] = "https://identity.test",
                ["Auth:IdentityInternalBaseUrl"] = internalBaseUrl,
                ["Auth:ClientId"] = "vaultly-web",
                ["Auth:ClientSecret"] = "secret",
                ["Auth:PublicOrigin"] = "https://app.test",
            })
            .Build();

        using var services = new ServiceCollection()
            .AddAuthFeature(configuration)
            .BuildServiceProvider();

        return services.GetRequiredService<IHttpClientFactory>()
            .CreateClient(nameof(IIdentityServiceClient))
            .BaseAddress;
    }
}
